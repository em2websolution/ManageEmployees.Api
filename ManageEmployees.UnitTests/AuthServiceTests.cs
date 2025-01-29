using FluentAssertions;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Exceptions;
using ManageEmployees.Domain.Interfaces.Repositories;
using ManageEmployees.Services.Services;
using ManageEmployees.Services.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private Mock<UserManager<User>> _userManagerMock;
    private Mock<ILogger<AuthService>> _loggerMock;
    private JwtSettings _jwtSettings;
    private AuthService _authService;
    private User _testUser;

    [SetUp]
    public void Setup()
    {
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _userManagerMock = MockUserManager();

        _jwtSettings = new JwtSettings
        {
            SecretKey = "supersecuresecretkey1234567890-asfdasdf-asdfasdf-asfasdf",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiresAt = "1"
        };

        _authService = new AuthService(
            _jwtSettings,
            _refreshTokenRepositoryMock.Object,
            _userManagerMock.Object,
            _loggerMock.Object
        );

        _testUser = new User
        {
            Id = "test-user-id",
            UserName = "testuser",
            Email = "testuser@example.com"
        };
    }

    private Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
    }

    [Test]
    public async Task GenerateTokenAsync_ShouldReturnToken_WhenUserExists()
    {
        // Arrange
        var roles = new List<string> { "Role1", "Role2" };
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.UserData, _testUser.Id)
    };

        _userManagerMock.Setup(m => m.FindByNameAsync(_testUser.UserName))
            .ReturnsAsync(_testUser);

        _userManagerMock.Setup(m => m.GetRolesAsync(_testUser))
            .ReturnsAsync(roles);

        _userManagerMock.Setup(m => m.GetClaimsAsync(_testUser))
            .ReturnsAsync(claims);

        _refreshTokenRepositoryMock.Setup(r => r.Create(It.IsAny<RefreshToken>()));
        _refreshTokenRepositoryMock.Setup(r => r.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        var token = await _authService.GenerateTokenAsync(_testUser.UserName);

        // Assert
        token.Should().NotBeNull();
        token.AccessToken.Should().NotBeNullOrEmpty();
        token.RefreshToken.Should().NotBeNullOrEmpty();

        _userManagerMock.Verify(m => m.FindByNameAsync(_testUser.UserName), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.Create(It.IsAny<RefreshToken>()), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.SaveAsync(), Times.Once);
    }

    [Test]
    public async Task RemoveRefreshTokenAsync_ShouldReturnFalse_WhenTokenNotFound()
    {
        // Arrange
        _refreshTokenRepositoryMock.Setup(r => r.GetRefreshTokenByUserId(_testUser.Id))
            .ReturnsAsync((RefreshToken)null);

        // Act
        var result = await _authService.RemoveRefreshTokenAsync(_testUser.Id);

        // Assert
        result.Should().BeFalse();

        _refreshTokenRepositoryMock.Verify(r => r.GetRefreshTokenByUserId(_testUser.Id), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.Delete(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Test]
    public async Task RefreshTokenSwapAsync_ShouldReturnNewToken_WhenRefreshTokenIsValid()
    {
        // Arrange
        var username = _testUser.UserName;
        var refreshToken = "valid-refresh-token";
        var dbToken = new RefreshToken
        {
            Token = refreshToken,
            UserId = _testUser.Id
        };

        _userManagerMock.Setup(m => m.FindByNameAsync(username))
            .ReturnsAsync(_testUser);

        _refreshTokenRepositoryMock.Setup(r => r.GetRefreshTokenByUserId(_testUser.Id))
            .ReturnsAsync(dbToken);

        _refreshTokenRepositoryMock.Setup(r => r.Delete(dbToken));
        _refreshTokenRepositoryMock.Setup(r => r.SaveAsync()).Returns(Task.CompletedTask);

        _userManagerMock.Setup(m => m.GetRolesAsync(_testUser))
            .ReturnsAsync(new List<string> { "User" });

        _userManagerMock.Setup(m => m.GetClaimsAsync(_testUser))
            .ReturnsAsync(new List<Claim>());

        // Act
        var token = await _authService.RefreshTokenSwapAsync(username, refreshToken);

        // Assert
        token.Should().NotBeNull();
        token.AccessToken.Should().NotBeNullOrEmpty();
        token.RefreshToken.Should().NotBeNullOrEmpty();

        _refreshTokenRepositoryMock.Verify(r => r.GetRefreshTokenByUserId(_testUser.Id), Times.AtLeastOnce);
        _refreshTokenRepositoryMock.Verify(r => r.Delete(dbToken), Times.AtLeastOnce);
        _refreshTokenRepositoryMock.Verify(r => r.SaveAsync(), Times.AtLeastOnce);
    }

    [Test]
    public async Task RefreshTokenSwapAsync_ShouldThrowException_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        var username = _testUser.UserName;
        var refreshToken = "invalid-refresh-token";
        var dbToken = new RefreshToken
        {
            Token = "valid-refresh-token",
            UserId = _testUser.Id
        };

        _userManagerMock.Setup(m => m.FindByNameAsync(username))
            .ReturnsAsync(_testUser);

        _refreshTokenRepositoryMock.Setup(r => r.GetRefreshTokenByUserId(_testUser.Id))
            .ReturnsAsync(dbToken);

        // Act
        Func<Task> act = async () => await _authService.RefreshTokenSwapAsync(username, refreshToken);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage($"Invalid RefreshToken || Method: RefreshTokenSwapAsync || TraceId: ");
    }

    [Test]
    public async Task RefreshTokenSwapAsync_ShouldThrowException_WhenValidationFails()
    {
        // Arrange
        var username = _testUser.UserName;
        var refreshToken = "valid-refresh-token";

        _userManagerMock.Setup(m => m.FindByNameAsync(username))
            .ReturnsAsync(_testUser);

        _refreshTokenRepositoryMock.Setup(r => r.GetRefreshTokenByUserId(_testUser.Id))
            .Throws(new Exception("Database error"));

        // Act
        Func<Task> act = async () => await _authService.RefreshTokenSwapAsync(username, refreshToken);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Database error");
    }

}
