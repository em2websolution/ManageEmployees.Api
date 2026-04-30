using FluentAssertions;
using ManageEmployees.Infra.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace ManageEmployees.IntegrationTests.Identity;

[TestFixture]
public class RoleStoreTests
{
    private RoleStore _store = null!;

    [SetUp]
    public async Task SetUp()
    {
        await DatabaseFixture.CleanTablesAsync();
        _store = new RoleStore(DatabaseFixture.ConnectionFactory);
    }

    [TearDown]
    public void TearDown()
    {
        _store.Dispose();
    }

    [Test]
    public async Task CreateAsync_ShouldInsertRole()
    {
        var role = CreateTestRole();

        await _store.CreateAsync(role, CancellationToken.None);

        var result = await _store.FindByIdAsync(role.Id!, CancellationToken.None);
        result.Should().NotBeNull();
        result!.Name.Should().Be(role.Name);
    }

    [Test]
    public async Task FindByIdAsync_WhenNotExists_ShouldReturnNull()
    {
        var result = await _store.FindByIdAsync(Guid.NewGuid().ToString(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Test]
    public async Task FindByNameAsync_ShouldReturnRole()
    {
        var role = CreateTestRole();
        await _store.CreateAsync(role, CancellationToken.None);

        var result = await _store.FindByNameAsync(role.NormalizedName!, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(role.Id);
        result.Name.Should().Be(role.Name);
    }

    [Test]
    public async Task FindByNameAsync_WhenNotExists_ShouldReturnNull()
    {
        var result = await _store.FindByNameAsync("NONEXISTENT", CancellationToken.None);

        result.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_ShouldModifyRole()
    {
        var role = CreateTestRole();
        await _store.CreateAsync(role, CancellationToken.None);

        role.Name = "UpdatedRole";
        role.NormalizedName = "UPDATEDROLE";
        await _store.UpdateAsync(role, CancellationToken.None);

        var result = await _store.FindByIdAsync(role.Id!, CancellationToken.None);
        result!.Name.Should().Be("UpdatedRole");
        result.NormalizedName.Should().Be("UPDATEDROLE");
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveRole()
    {
        var role = CreateTestRole();
        await _store.CreateAsync(role, CancellationToken.None);

        await _store.DeleteAsync(role, CancellationToken.None);

        var result = await _store.FindByIdAsync(role.Id!, CancellationToken.None);
        result.Should().BeNull();
    }

    [Test]
    public async Task GetRoleIdAsync_ShouldReturnId()
    {
        var role = CreateTestRole();

        var result = await _store.GetRoleIdAsync(role, CancellationToken.None);

        result.Should().Be(role.Id);
    }

    [Test]
    public async Task GetRoleNameAsync_ShouldReturnName()
    {
        var role = CreateTestRole();

        var result = await _store.GetRoleNameAsync(role, CancellationToken.None);

        result.Should().Be(role.Name);
    }

    [Test]
    public async Task SetRoleNameAsync_ShouldSetName()
    {
        var role = CreateTestRole();

        await _store.SetRoleNameAsync(role, "NewRoleName", CancellationToken.None);

        role.Name.Should().Be("NewRoleName");
    }

    [Test]
    public async Task GetNormalizedRoleNameAsync_ShouldReturnNormalizedName()
    {
        var role = CreateTestRole();

        var result = await _store.GetNormalizedRoleNameAsync(role, CancellationToken.None);

        result.Should().Be(role.NormalizedName);
    }

    [Test]
    public async Task SetNormalizedRoleNameAsync_ShouldSetValue()
    {
        var role = CreateTestRole();

        await _store.SetNormalizedRoleNameAsync(role, "NEWVALUE", CancellationToken.None);

        role.NormalizedName.Should().Be("NEWVALUE");
    }

    [Test]
    public void Dispose_ShouldNotThrow()
    {
        var store = new RoleStore(DatabaseFixture.ConnectionFactory);

        var act = () => store.Dispose();

        act.Should().NotThrow();
    }

    private static IdentityRole CreateTestRole(string name = "TestRole") => new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        ConcurrencyStamp = Guid.NewGuid().ToString()
    };
}
