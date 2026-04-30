using FluentAssertions;
using ManageEmployees.Infra.Data.Connection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class SqlConnectionFactoryTests
{
    private static IConfiguration BuildConfiguration(string? connectionString)
    {
        var kvp = connectionString is not null
            ? new Dictionary<string, string?> { { "ConnectionStrings:DBConnection", connectionString } }
            : new Dictionary<string, string?>();

        return new ConfigurationBuilder()
            .AddInMemoryCollection(kvp)
            .Build();
    }

    [Test]
    public void Constructor_ShouldThrow_WhenConnectionStringIsMissing()
    {
        // Arrange
        var configuration = BuildConfiguration(null);

        // Act
        var act = () => new SqlConnectionFactory(configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_ShouldSucceed_WhenConnectionStringIsPresent()
    {
        // Arrange
        var configuration = BuildConfiguration("Server=localhost;Database=Test;");

        // Act
        var act = () => new SqlConnectionFactory(configuration);

        // Assert
        act.Should().NotThrow();
    }

    [Test]
    public void CreateConnection_ShouldReturnSqlConnection()
    {
        // Arrange
        var configuration = BuildConfiguration("Server=localhost;Database=Test;");
        var factory = new SqlConnectionFactory(configuration);

        // Act
        var connection = factory.CreateConnection();

        // Assert
        connection.Should().NotBeNull();
        connection.Should().BeOfType<SqlConnection>();
    }
}
