using FluentAssertions;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Infra.Data.Identity;
using Microsoft.Data.SqlClient;

namespace ManageEmployees.IntegrationTests.Identity;

[TestFixture]
public class UserStoreTests
{
    private UserStore _store = null!;

    [SetUp]
    public async Task SetUp()
    {
        await DatabaseFixture.CleanTablesAsync();
        _store = new UserStore(DatabaseFixture.ConnectionFactory);
    }

    [TearDown]
    public void TearDown()
    {
        _store.Dispose();
    }

    #region IUserStore

    [Test]
    public async Task CreateAsync_ShouldInsertUser()
    {
        var user = CreateTestUser();

        await _store.CreateAsync(user, CancellationToken.None);

        var result = await _store.FindByIdAsync(user.Id, CancellationToken.None);
        result.Should().NotBeNull();
        result!.Email.Should().Be(user.Email);
        result.FirstName.Should().Be("Test");
        result.LastName.Should().Be("User");
    }

    [Test]
    public async Task FindByIdAsync_WhenNotExists_ShouldReturnNull()
    {
        var result = await _store.FindByIdAsync(Guid.NewGuid().ToString(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Test]
    public async Task FindByNameAsync_ShouldReturnUser()
    {
        var user = CreateTestUser();
        await _store.CreateAsync(user, CancellationToken.None);

        var result = await _store.FindByNameAsync(user.NormalizedUserName!, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Test]
    public async Task FindByNameAsync_WhenNotExists_ShouldReturnNull()
    {
        var result = await _store.FindByNameAsync("NONEXISTENT@TEST.COM", CancellationToken.None);

        result.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_ShouldModifyUser()
    {
        var user = CreateTestUser();
        await _store.CreateAsync(user, CancellationToken.None);

        user.FirstName = "Updated";
        user.LastName = "Name";
        user.PhoneNumber = "11999998888";
        await _store.UpdateAsync(user, CancellationToken.None);

        var result = await _store.FindByIdAsync(user.Id, CancellationToken.None);
        result!.FirstName.Should().Be("Updated");
        result.LastName.Should().Be("Name");
        result.PhoneNumber.Should().Be("11999998888");
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveUser()
    {
        var user = CreateTestUser();
        await _store.CreateAsync(user, CancellationToken.None);

        await _store.DeleteAsync(user, CancellationToken.None);

        var result = await _store.FindByIdAsync(user.Id, CancellationToken.None);
        result.Should().BeNull();
    }

    [Test]
    public async Task GetUserIdAsync_ShouldReturnId()
    {
        var user = CreateTestUser();

        var result = await _store.GetUserIdAsync(user, CancellationToken.None);

        result.Should().Be(user.Id);
    }

    [Test]
    public async Task GetUserNameAsync_ShouldReturnUserName()
    {
        var user = CreateTestUser();

        var result = await _store.GetUserNameAsync(user, CancellationToken.None);

        result.Should().Be(user.UserName);
    }

    [Test]
    public async Task SetUserNameAsync_ShouldSetUserName()
    {
        var user = CreateTestUser();

        await _store.SetUserNameAsync(user, "newname@test.com", CancellationToken.None);

        user.UserName.Should().Be("newname@test.com");
    }

    [Test]
    public async Task GetNormalizedUserNameAsync_ShouldReturnNormalizedUserName()
    {
        var user = CreateTestUser();

        var result = await _store.GetNormalizedUserNameAsync(user, CancellationToken.None);

        result.Should().Be(user.NormalizedUserName);
    }

    [Test]
    public async Task SetNormalizedUserNameAsync_ShouldSetValue()
    {
        var user = CreateTestUser();

        await _store.SetNormalizedUserNameAsync(user, "NEWVALUE@TEST.COM", CancellationToken.None);

        user.NormalizedUserName.Should().Be("NEWVALUE@TEST.COM");
    }

    #endregion

    #region IUserPasswordStore

    [Test]
    public async Task SetPasswordHashAsync_ShouldSetHash()
    {
        var user = CreateTestUser();
        var hash = "hashed_password_123";

        await _store.SetPasswordHashAsync(user, hash, CancellationToken.None);

        user.PasswordHash.Should().Be(hash);
    }

    [Test]
    public async Task GetPasswordHashAsync_ShouldReturnHash()
    {
        var user = CreateTestUser();
        user.PasswordHash = "hashed_password_456";

        var result = await _store.GetPasswordHashAsync(user, CancellationToken.None);

        result.Should().Be("hashed_password_456");
    }

    [Test]
    public async Task HasPasswordAsync_ShouldReturnTrue_WhenHashSet()
    {
        var user = CreateTestUser();
        user.PasswordHash = "some_hash";

        var result = await _store.HasPasswordAsync(user, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Test]
    public async Task HasPasswordAsync_ShouldReturnFalse_WhenHashNull()
    {
        var user = CreateTestUser();
        user.PasswordHash = null;

        var result = await _store.HasPasswordAsync(user, CancellationToken.None);

        result.Should().BeFalse();
    }

    #endregion

    #region IUserRoleStore

    [Test]
    public async Task AddToRoleAsync_ShouldAssignRole()
    {
        var user = CreateTestUser();
        await _store.CreateAsync(user, CancellationToken.None);
        await SeedRoleAsync("Manager");

        await _store.AddToRoleAsync(user, "MANAGER", CancellationToken.None);

        var roles = await _store.GetRolesAsync(user, CancellationToken.None);
        roles.Should().Contain("Manager");
    }

    [Test]
    public async Task RemoveFromRoleAsync_ShouldRemoveRole()
    {
        var user = CreateTestUser();
        await _store.CreateAsync(user, CancellationToken.None);
        await SeedRoleAsync("Employee");
        await _store.AddToRoleAsync(user, "EMPLOYEE", CancellationToken.None);

        await _store.RemoveFromRoleAsync(user, "EMPLOYEE", CancellationToken.None);

        var roles = await _store.GetRolesAsync(user, CancellationToken.None);
        roles.Should().NotContain("Employee");
    }

    [Test]
    public async Task GetRolesAsync_ShouldReturnAssignedRoles()
    {
        var user = CreateTestUser();
        await _store.CreateAsync(user, CancellationToken.None);
        await SeedRoleAsync("Admin");
        await SeedRoleAsync("Editor");
        await _store.AddToRoleAsync(user, "ADMIN", CancellationToken.None);
        await _store.AddToRoleAsync(user, "EDITOR", CancellationToken.None);

        var roles = await _store.GetRolesAsync(user, CancellationToken.None);

        roles.Should().HaveCount(2);
        roles.Should().Contain("Admin");
        roles.Should().Contain("Editor");
    }

    [Test]
    public async Task IsInRoleAsync_ShouldReturnTrue_WhenInRole()
    {
        var user = CreateTestUser();
        await _store.CreateAsync(user, CancellationToken.None);
        await SeedRoleAsync("Developer");
        await _store.AddToRoleAsync(user, "DEVELOPER", CancellationToken.None);

        var result = await _store.IsInRoleAsync(user, "DEVELOPER", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Test]
    public async Task IsInRoleAsync_ShouldReturnFalse_WhenNotInRole()
    {
        var user = CreateTestUser();
        await _store.CreateAsync(user, CancellationToken.None);
        await SeedRoleAsync("Tester");

        var result = await _store.IsInRoleAsync(user, "TESTER", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    public async Task GetUsersInRoleAsync_ShouldReturnUsersInRole()
    {
        var user1 = CreateTestUser("user1@test.com");
        var user2 = CreateTestUser("user2@test.com");
        var user3 = CreateTestUser("user3@test.com");
        await _store.CreateAsync(user1, CancellationToken.None);
        await _store.CreateAsync(user2, CancellationToken.None);
        await _store.CreateAsync(user3, CancellationToken.None);
        await SeedRoleAsync("Analyst");
        await _store.AddToRoleAsync(user1, "ANALYST", CancellationToken.None);
        await _store.AddToRoleAsync(user3, "ANALYST", CancellationToken.None);

        var result = await _store.GetUsersInRoleAsync("ANALYST", CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(u => u.Email).Should().Contain("user1@test.com");
        result.Select(u => u.Email).Should().Contain("user3@test.com");
    }

    #endregion

    #region IUserSecurityStampStore

    [Test]
    public async Task SetSecurityStampAsync_ShouldSetStamp()
    {
        var user = CreateTestUser();
        var stamp = "new-security-stamp";

        await _store.SetSecurityStampAsync(user, stamp, CancellationToken.None);

        user.SecurityStamp.Should().Be(stamp);
    }

    [Test]
    public async Task GetSecurityStampAsync_ShouldReturnStamp()
    {
        var user = CreateTestUser();
        user.SecurityStamp = "my-stamp";

        var result = await _store.GetSecurityStampAsync(user, CancellationToken.None);

        result.Should().Be("my-stamp");
    }

    #endregion

    #region IDisposable

    [Test]
    public void Dispose_ShouldNotThrow()
    {
        var store = new UserStore(DatabaseFixture.ConnectionFactory);

        var act = () => store.Dispose();

        act.Should().NotThrow();
    }

    #endregion

    private static User CreateTestUser(string email = "test@test.com") => new()
    {
        Id = Guid.NewGuid().ToString(),
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        EmailConfirmed = true,
        SecurityStamp = Guid.NewGuid().ToString(),
        ConcurrencyStamp = Guid.NewGuid().ToString(),
        PhoneNumberConfirmed = false,
        TwoFactorEnabled = false,
        LockoutEnabled = false,
        AccessFailedCount = 0,
        FirstName = "Test",
        LastName = "User",
        DocNumber = "12345678900"
    };

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
}
