using ManageEmployees.Infra.Data.Connection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ManageEmployees.IntegrationTests;

/// <summary>
/// Creates and manages a test database for integration tests.
/// Runs once per test assembly via [SetUpFixture].
/// </summary>
[SetUpFixture]
public partial class DatabaseFixture
{
    public static IDbConnectionFactory ConnectionFactory { get; private set; } = null!;
    public static string ConnectionString { get; private set; } = null!;

    private static string _databaseName = null!;
    private static string _masterConnectionString = null!;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
            .AddJsonFile("appsettings.Test.json")
            .Build();

        ConnectionString = config.GetConnectionString("DBConnection")!;

        var builder = new SqlConnectionStringBuilder(ConnectionString);
        _databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";
        _masterConnectionString = builder.ConnectionString;

        await CreateDatabaseAsync();
        await CreateSchemaAsync();

        ConnectionFactory = new TestConnectionFactory(ConnectionString);
    }

    [OneTimeTearDown]
    public async Task GlobalTeardown()
    {
        await DropDatabaseAsync();
    }

    /// <summary>
    /// Cleans all data from test tables. Called between tests.
    /// </summary>
#pragma warning disable NUnit1028
    internal static async Task CleanTablesAsync()
#pragma warning restore NUnit1028
    {
        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        var tables = new[] { "Tasks", "RefreshTokens", "UserRoles", "Roles", "Users" };
        foreach (var table in tables)
        {
            using var cmd = new SqlCommand($"DELETE FROM [{table}]", connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task CreateDatabaseAsync()
    {
        using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
            IF DB_ID('{_databaseName}') IS NULL
                CREATE DATABASE [{_databaseName}]";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task CreateSchemaAsync()
    {
        var infraAssembly = typeof(SqlConnectionFactory).Assembly;
        var resourceName = infraAssembly.GetManifestResourceNames()
            .First(r => r.EndsWith("001_CreateTables.sql"));

        using var stream = infraAssembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync();

        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        var batches = GoStatementRegex().Split(sql);

        foreach (var batch in batches)
        {
            var trimmed = batch.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            using var cmd = new SqlCommand(trimmed, connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task DropDatabaseAsync()
    {
        using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
            IF DB_ID('{_databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{_databaseName}];
            END";
        await cmd.ExecuteNonQueryAsync();
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoStatementRegex();
}

/// <summary>
/// Simple IDbConnectionFactory for integration tests.
/// </summary>
internal class TestConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public TestConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqlConnection CreateConnection() => new(_connectionString);
}
