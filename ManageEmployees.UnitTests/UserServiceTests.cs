using FluentAssertions;
using ManageEmployees.Domain;
using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Exceptions;
using ManageEmployees.Domain.Interfaces.Repositories;
using ManageEmployees.Domain.Interfaces.Services;
using ManageEmployees.Domain.Models;
using ManageEmployees.Services.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class UserServiceTests
{
    private Mock<UserManager<User>> _userManagerMock;
    private Mock<IAuthService> _authServiceMock;
    private Mock<ILogger<UserService>> _loggerMock;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private Mock<IUserRepository> _userRepositoryMock;
    private UserService _userService;
    private const string USER = "user";
    private User _currentUser;
    private string _currentUserId;
    private User _existingUser;
    private string _existingUserId;
    private UpdateUser _updateUser;
    private CreateUser _createUser;

    [SetUp]
    public void Setup()
    {
        _userManagerMock = MockUserManager();
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _userService = new UserService(
            _userManagerMock.Object,
            _authServiceMock.Object,
            _loggerMock.Object,
            _httpContextAccessorMock.Object,
            _userRepositoryMock.Object
        );

        _currentUserId = "current-user-id";
        _existingUserId = "existing-user-id";
        _currentUser = new User { Id = _currentUserId, Email = "currentuser@example.com", UserName = "currentuser@example.com" };
        _existingUser = new User { Id = _existingUserId, Email = "existinguser@example.com" };
        _updateUser = new UpdateUser
        {
            FirstName = "Updated",
            LastName = "User",
            Email = "updateduser@example.com",
            DocNumber = "123456789",
            Role = RoleName.Employee,
            Password = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };
        _createUser = new CreateUser
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "johndoe@example.com",
            Password = "StrongPassword123!",
            ConfirmPassword = "StrongPassword123!",
            DocNumber = "123456789",
            Role = RoleName.Employee
        };

        SetupHttpContext(_currentUserId);
    }

    private static Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private void SetupHttpContext(string userId)
    {
        var httpContext = new DefaultHttpContext();

        var responseCookiesMock = new Mock<IResponseCookies>();
        responseCookiesMock.Setup(c => c.Append(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CookieOptions>()));

        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        _httpContextAccessorMock.Setup(a => a.HttpContext!.Request.Cookies[USER]).Returns(userId);
        _httpContextAccessorMock.Setup(a => a.HttpContext!.Response.Cookies).Returns(responseCookiesMock.Object);
    }

    [Test]
    public async Task SignUpAsync_ShouldCreateUser_WhenDataIsValid()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByNameAsync(_createUser.Email.ToLower()))
            .ReturnsAsync((User?)null);

        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), _createUser.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), _createUser.Role))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.SignUpAsync(_createUser);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Be("User created successfully!");

        _userManagerMock.Verify(m => m.CreateAsync(It.Is<User>(u =>
            string.Equals(u.Email, _createUser.Email, StringComparison.OrdinalIgnoreCase) &&
            u.FirstName == _createUser.FirstName &&
            u.LastName == _createUser.LastName &&
            u.DocNumber == _createUser.DocNumber
        ), _createUser.Password), Times.Once);

        _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), _createUser.Role), Times.Once);
    }

    [Test]
    public async Task SignInAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        var credentials = new NetworkCredential("nonexistentuser@example.com", "password123");

        _userManagerMock.Setup(m => m.FindByNameAsync(credentials.UserName))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _userService.SignInAsync(credentials);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"User {credentials.UserName} not found!");
    }

    [Test]
    public async Task SignInAsync_ShouldReturnToken_WhenLoginIsSuccessful()
    {
        // Arrange
        var credentials = new NetworkCredential(_currentUser.UserName, "password123");
        var token = new Token
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token"
        };

        _userManagerMock.Setup(m => m.FindByNameAsync(_currentUser.UserName!.ToLower()))
            .ReturnsAsync(_currentUser);

        _userManagerMock.Setup(m => m.CheckPasswordAsync(_currentUser, credentials.Password))
            .ReturnsAsync(true);

        _userManagerMock.Setup(m => m.GetRolesAsync(_currentUser))
            .ReturnsAsync(new List<string> { RoleName.Employee });

        _authServiceMock.Setup(a => a.GenerateTokenAsync(_currentUser.UserName!.ToLower()))
            .ReturnsAsync(token);

        // Act
        var result = await _userService.SignInAsync(credentials);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(token.AccessToken);
        result.RefreshToken.Should().Be(token.RefreshToken);
    }

    [Test]
    public async Task SignOutAsync_ShouldSignOutSuccessfully_WhenUserIdIsPresentInCookies()
    {
        // Arrange
        SetupHttpContext(_currentUserId);

        _authServiceMock.Setup(a => a.RemoveRefreshTokenAsync(_currentUserId))
            .ReturnsAsync(true);

        var responseCookiesMock = new Mock<IResponseCookies>();
        responseCookiesMock.Setup(c => c.Delete(It.IsAny<string>()));

        _httpContextAccessorMock.Setup(a => a.HttpContext!.Response.Cookies)
            .Returns(responseCookiesMock.Object);

        // Act
        var result = await _userService.SignOutAsync();

        // Assert
        result.Should().BeTrue();

        _authServiceMock.Verify(a => a.RemoveRefreshTokenAsync(_currentUserId), Times.Once);

        responseCookiesMock.Verify(c => c.Delete("access_token"), Times.Once);
        responseCookiesMock.Verify(c => c.Delete("refresh_token"), Times.Once);
        responseCookiesMock.Verify(c => c.Delete(USER), Times.Once);
    }

    [Test]
    public async Task SignOutAsync_ShouldReturnFalse_WhenExceptionOccurs()
    {
        // Arrange
        _authServiceMock.Setup(a => a.RemoveRefreshTokenAsync(_currentUserId))
            .ThrowsAsync(new Exception("Failed to remove refresh token."));

        var responseCookiesMock = new Mock<IResponseCookies>();
        responseCookiesMock.Setup(c => c.Delete(It.IsAny<string>()));

        _httpContextAccessorMock.Setup(a => a.HttpContext!.Response.Cookies)
            .Returns(responseCookiesMock.Object);

        // Act
        var result = await _userService.SignOutAsync();

        // Assert
        result.Should().BeFalse();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error occurred during sign out.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Test]
    public async Task SignUpAsync_ShouldThrowBusinessException_WhenRoleIsInvalid()
    {
        // Arrange
        _createUser.Role = "InvalidRole";

        // Act
        Func<Task> act = async () => await _userService.SignUpAsync(_createUser);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Invalid role*");
    }

    [Test]
    public async Task SignUpAsync_ShouldThrowBusinessException_WhenEmailAlreadyExists()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByNameAsync(_createUser.Email.ToLower()))
            .ReturnsAsync(new User { Email = _createUser.Email.ToLower() });

        // Act
        Func<Task> act = async () => await _userService.SignUpAsync(_createUser);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*already exists*");
    }

    [Test]
    public async Task UpdateUserAsync_ShouldThrowException_WhenUpdateFails()
    {
        // Arrange
        var passwordHasherMock = new Mock<IPasswordHasher<User>>();
        passwordHasherMock.Setup(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
            .Returns("hashed-password");
        _userManagerMock.Object.PasswordHasher = passwordHasherMock.Object;

        _userManagerMock.Setup(m => m.FindByIdAsync(_currentUserId))
            .ReturnsAsync(_existingUser);

        _userManagerMock.Setup(m => m.GetRolesAsync(_existingUser))
            .ReturnsAsync(new List<string> { RoleName.Employee });

        _userManagerMock.Setup(m => m.UpdateAsync(_existingUser))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed." }));

        // Act
        var act = async () => await _userService.UpdateUserAsync(_currentUserId, _updateUser);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Failed to update user: Update failed.");
    }

    [Test]
    public async Task SignUpAsync_ShouldThrow_WhenUnhandledExceptionOccurs()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByNameAsync(_createUser.Email.ToLower()))
            .ReturnsAsync((User?)null);

        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), _createUser.Password))
            .Throws(new InvalidOperationException("An unexpected error occurred."));

        // Act
        var act = async () => await _userService.SignUpAsync(_createUser);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("An unexpected error occurred.");
    }

    [Test]
    public async Task UpdateUserAsync_ShouldUpdateUser_WhenDataIsValid()
    {
        // Arrange
        var passwordHasherMock = new Mock<IPasswordHasher<User>>();
        passwordHasherMock.Setup(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
            .Returns("hashed-password");
        _userManagerMock.Object.PasswordHasher = passwordHasherMock.Object;

        _userManagerMock.Setup(m => m.FindByIdAsync(_currentUserId))
            .ReturnsAsync(_existingUser);

        _userManagerMock.Setup(m => m.GetRolesAsync(_existingUser))
            .ReturnsAsync(new List<string> { RoleName.Employee });

        _userManagerMock.Setup(m => m.UpdateAsync(_existingUser))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.UpdateUserAsync(_currentUserId, _updateUser);

        // Assert
        result.Should().BeTrue();
        _existingUser.FirstName.Should().Be("Updated");
        _existingUser.LastName.Should().Be("User");
        _existingUser.Email.Should().Be("updateduser@example.com");
    }

    [Test]
    public async Task UpdateUserAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        var userId = "nonexistent-user-id";

        _userManagerMock.Setup(m => m.FindByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _userService.UpdateUserAsync(userId, _updateUser);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"User with ID {userId} not found!");
    }

    [Test]
    public async Task DeleteUserAsync_ShouldThrowException_WhenDeleteFails()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByIdAsync(_currentUserId))
            .ReturnsAsync(_existingUser);

        _userManagerMock.Setup(m => m.DeleteAsync(_existingUser))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete failed." }));

        // Act
        var act = async () => await _userService.DeleteUserAsync(_currentUserId);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Failed to delete user: Delete failed.");
    }

    [Test]
    public async Task DeleteUserAsync_ShouldDeleteUser_WhenUserExists()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByIdAsync(_currentUserId))
            .ReturnsAsync(_existingUser);

        _userManagerMock.Setup(m => m.DeleteAsync(_existingUser))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.DeleteUserAsync(_currentUserId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task DeleteUserAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByIdAsync(_currentUserId))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _userService.DeleteUserAsync(_currentUserId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"User with ID {_currentUserId} not found!");
    }

    [Test]
    public async Task GetAllUsersAsync_ShouldReturnAllUsers()
    {
        // Arrange
        var users = new List<UserDto>
        {
            new UserDto
            {
                UserId = "1",
                Email = "user1@example.com",
                FirstName = "John",
                LastName = "Doe",
                DocNumber = "123456",
                PhoneNumber = "123456789",
                Role = RoleName.Employee
            },
            new UserDto
            {
                UserId = "2",
                Email = "user2@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                DocNumber = "654321",
                PhoneNumber = "111222333",
                Role = RoleName.Administrator
            }
        };

        var pagedResult = new PagedResult<UserDto>
        {
            Items = users,
            Page = 1,
            PageSize = 10,
            TotalCount = 2
        };

        _userRepositoryMock.Setup(r => r.GetAllWithRolesAsync(1, 10, null, null))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _userService.GetAllUsersAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);

        var firstUser = result.Items.FirstOrDefault(u => u.UserId == "1");
        firstUser.Should().NotBeNull();
        firstUser!.FirstName.Should().Be("John");
        firstUser.LastName.Should().Be("Doe");
        firstUser.Email.Should().Be("user1@example.com");
        firstUser.DocNumber.Should().Be("123456");

        var secondUser = result.Items.FirstOrDefault(u => u.UserId == "2");
        secondUser.Should().NotBeNull();
        secondUser!.FirstName.Should().Be("Jane");
        secondUser.LastName.Should().Be("Smith");
        secondUser.Email.Should().Be("user2@example.com");
        secondUser.DocNumber.Should().Be("654321");
    }
}