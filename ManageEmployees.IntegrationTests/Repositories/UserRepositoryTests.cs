using FluentAssertions;
using ManageEmployees.Infra.Data.Repositories;
using ManageEmployees.IntegrationTests;
using Microsoft.Data.SqlClient;

namespace ManageEmployees.IntegrationTests.Repositories;

[TestFixture]
public class UserRepositoryTests
{
    private UserRepository _repository = null!;

    [SetUp]
    public async Task SetUp()
    {
        await DatabaseFixture.CleanTablesAsync();
        _repository = new UserRepository(DatabaseFixture.ConnectionFactory);
    }

    [Test]
    public async Task GetAllWithRolesAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        var result = await _repository.GetAllWithRolesAsync(1, 100);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Test]
    public async Task GetAllWithRolesAsync_ShouldReturnUsersWithRoles()
    {
        var userId = await SeedUserAsync("john@test.com", "John", "Doe");
        var roleId = await SeedRoleAsync("Employee");
        await AssignRoleAsync(userId, roleId);

        var result = await _repository.GetAllWithRolesAsync(1, 100);

        result.Items.Should().HaveCount(1);
        result.Items[0].FirstName.Should().Be("John");
        result.Items[0].LastName.Should().Be("Doe");
        result.Items[0].Email.Should().Be("john@test.com");
        result.Items[0].Role.Should().Be("Employee");
    }

    [Test]
    public async Task GetAllWithRolesAsync_UserWithNoRole_ShouldReturnEmptyRole()
    {
        await SeedUserAsync("norole@test.com", "No", "Role");

        var result = await _repository.GetAllWithRolesAsync(1, 100);

        result.Items.Should().HaveCount(1);
        result.Items[0].Role.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllWithRolesAsync_ShouldReturnOrderedByFirstName()
    {
        await SeedUserAsync("zara@test.com", "Zara", "Smith");
        await SeedUserAsync("alice@test.com", "Alice", "Johnson");
        await SeedUserAsync("mike@test.com", "Mike", "Brown");

        var result = await _repository.GetAllWithRolesAsync(1, 100);

        result.Items.Should().HaveCount(3);
        result.Items[0].FirstName.Should().Be("Alice");
        result.Items[1].FirstName.Should().Be("Mike");
        result.Items[2].FirstName.Should().Be("Zara");
    }

    [Test]
    public async Task GetAllWithRolesAsync_ShouldReturnDocNumberAndPhoneNumber()
    {
        await SeedUserAsync("full@test.com", "Full", "Data", "99988877766", "11999998888");

        var result = await _repository.GetAllWithRolesAsync(1, 100);

        result.Items[0].DocNumber.Should().Be("99988877766");
        result.Items[0].PhoneNumber.Should().Be("11999998888");
    }

    [Test]
    public async Task GetAllWithRolesAsync_NullPhoneNumber_ShouldReturnNull()
    {
        await SeedUserAsync("nophone@test.com", "No", "Phone");

        var result = await _repository.GetAllWithRolesAsync(1, 100);

        result.Items[0].PhoneNumber.Should().BeNull();
    }

    private static async Task<string> SeedUserAsync(string email, string firstName, string lastName,
        string docNumber = "12345678900", string? phoneNumber = null)
    {
        var userId = Guid.NewGuid().ToString();
        using var connection = new SqlConnection(DatabaseFixture.ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO Users (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
                               SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
                               TwoFactorEnabled, LockoutEnabled, AccessFailedCount,
                               FirstName, LastName, DocNumber)
            VALUES (@Id, @Email, @NormalizedEmail, @Email, @NormalizedEmail, 0,
                    @Stamp, @Stamp, @Phone, 0, 0, 0, 0, @FirstName, @LastName, @DocNumber)";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", userId);
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@NormalizedEmail", email.ToUpperInvariant());
        cmd.Parameters.AddWithValue("@Stamp", Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("@Phone", (object?)phoneNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FirstName", firstName);
        cmd.Parameters.AddWithValue("@LastName", lastName);
        cmd.Parameters.AddWithValue("@DocNumber", docNumber);
        await cmd.ExecuteNonQueryAsync();

        return userId;
    }

    private static async Task<string> SeedRoleAsync(string roleName)
    {
        var roleId = Guid.NewGuid().ToString();
        using var connection = new SqlConnection(DatabaseFixture.ConnectionString);
        await connection.OpenAsync();

        const string sql = "INSERT INTO Roles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (@Id, @Name, @NormalizedName, @Stamp)";
        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", roleId);
        cmd.Parameters.AddWithValue("@Name", roleName);
        cmd.Parameters.AddWithValue("@NormalizedName", roleName.ToUpperInvariant());
        cmd.Parameters.AddWithValue("@Stamp", Guid.NewGuid().ToString());
        await cmd.ExecuteNonQueryAsync();

        return roleId;
    }

    private static async Task AssignRoleAsync(string userId, string roleId)
    {
        using var connection = new SqlConnection(DatabaseFixture.ConnectionString);
        await connection.OpenAsync();

        const string sql = "INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)";
        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@RoleId", roleId);
        await cmd.ExecuteNonQueryAsync();
    }
}
