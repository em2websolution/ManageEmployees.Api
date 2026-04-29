using FluentAssertions;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Infra.Data.Repositories;
using ManageEmployees.IntegrationTests;
using Microsoft.Data.SqlClient;

namespace ManageEmployees.IntegrationTests.Repositories;

[TestFixture]
public class RefreshTokenRepositoryTests
{
    private RefreshTokenRepository _repository = null!;
    private string _testUserId = null!;

    [SetUp]
    public async Task SetUp()
    {
        await DatabaseFixture.CleanTablesAsync();
        _repository = new RefreshTokenRepository(DatabaseFixture.ConnectionFactory);
        _testUserId = await SeedTestUserAsync();
    }

    [Test]
    public async Task CreateAsync_ShouldInsertToken()
    {
        var token = CreateRefreshToken();

        await _repository.CreateAsync(token);

        var result = await _repository.GetByUserIdAsync(_testUserId);
        result.Should().NotBeNull();
        result!.Token.Should().Be(token.Token);
        result.UserId.Should().Be(_testUserId);
    }

    [Test]
    public async Task GetByUserIdAsync_WhenExists_ShouldReturnToken()
    {
        var token = CreateRefreshToken();
        await _repository.CreateAsync(token);

        var result = await _repository.GetByUserIdAsync(_testUserId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(token.Id);
        result.ExpireDate.Should().BeCloseTo(token.ExpireDate, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task GetByUserIdAsync_WhenNotExists_ShouldReturnNull()
    {
        var result = await _repository.GetByUserIdAsync("nonexistent-user");

        result.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveToken()
    {
        var token = CreateRefreshToken();
        await _repository.CreateAsync(token);

        await _repository.DeleteAsync(token.Id);

        var result = await _repository.GetByUserIdAsync(_testUserId);
        result.Should().BeNull();
    }

    [Test]
    public async Task CreateAsync_MultipleTimes_ShouldStoreMultipleTokens()
    {
        var secondUserId = await SeedTestUserAsync();

        var token1 = CreateRefreshToken();
        var token2 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = secondUserId,
            Token = "second-refresh-token-value",
            ExpireDate = DateTime.UtcNow.AddDays(14)
        };

        await _repository.CreateAsync(token1);
        await _repository.CreateAsync(token2);

        var result1 = await _repository.GetByUserIdAsync(_testUserId);
        result1.Should().NotBeNull();

        var result2 = await _repository.GetByUserIdAsync(secondUserId);
        result2.Should().NotBeNull();
    }

    private RefreshToken CreateRefreshToken() => new()
    {
        Id = Guid.NewGuid(),
        UserId = _testUserId,
        Token = "test-refresh-token-value",
        ExpireDate = DateTime.UtcNow.AddDays(7)
    };

    private static async Task<string> SeedTestUserAsync()
    {
        var userId = Guid.NewGuid().ToString();
        var email = $"token-{userId[..8]}@test.com";
        var normalizedEmail = email.ToUpperInvariant();

        using var connection = new SqlConnection(DatabaseFixture.ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO Users (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
                               SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
                               LockoutEnabled, AccessFailedCount, FirstName, LastName, DocNumber)
            VALUES (@Id, @Email, @NormalizedEmail, @Email, @NormalizedEmail, 0,
                    @Stamp, @Stamp, 0, 0, 0, 0, 'Token', 'User', '99988877766')";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", userId);
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@NormalizedEmail", normalizedEmail);
        cmd.Parameters.AddWithValue("@Stamp", Guid.NewGuid().ToString());
        await cmd.ExecuteNonQueryAsync();

        return userId;
    }
}
