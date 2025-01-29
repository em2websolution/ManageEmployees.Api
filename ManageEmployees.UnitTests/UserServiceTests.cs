using FluentAssertions;
using ManageEmployees.Domain;
using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Exceptions;
using ManageEmployees.Domain.Interfaces.Services;
using ManageEmployees.Domain.Models;
using ManageEmployees.Services.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class UserServiceTests
{
    private Mock<UserManager<User>> _userManagerMock;
    private Mock<IAuthService> _authServiceMock;
    private Mock<ILogger<UserService>> _loggerMock;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private Mock<IEncryptionService> _encryptionServiceMock;
    private UserService _userService;
    private const string USER = "user";
    private User _currentUser;
    private string _currentUserId;
    private User _existingUser;
    private string _existingUserId;
    private UpdateUser _updateUser;
    private CreateUser _createUser;
    private NetworkCredential _credentials;

    [SetUp]
    public void Setup()
    {
        _userManagerMock = MockUserManager();
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _encryptionServiceMock = new Mock<IEncryptionService>();

        _userService = new UserService(
            _userManagerMock.Object,
            _authServiceMock.Object,
            _loggerMock.Object,
            _httpContextAccessorMock.Object,
            _encryptionServiceMock.Object
        );

        _currentUserId = "current-user-id";
        _existingUserId = "existing-user-id";
        _currentUser = new User { Id = _currentUserId, Email = "currentuser@example.com", UserName = "johndoe@example.com" };
        _existingUser = new User { Id = _existingUserId, Email = "existinguser@example.com" };
        _updateUser = new UpdateUser
        {
            FirstName = "Updated",
            LastName = "User",
            Email = "updateduser@example.com",
            DocNumber = "123456789",
            ManagerId = null,
            Role = RoleName.Employee
        };
        _createUser = new CreateUser
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "johndoe@example.com",
            Password = "StrongPassword123!",
            ConfirmPassword = "StrongPassword123!",
            DocNumber = "123456789",
            ManagerId = _currentUserId,
            Role = RoleName.Employee
        };

        _credentials = new NetworkCredential(_createUser.Email, _createUser.Password);

        SetupHttpContext(_currentUserId);
        SetupUserRoles(_userManagerMock, _currentUser, new List<string> { RoleName.Director });
        SetupUserRoles(_userManagerMock, _existingUser, new List<string> { RoleName.Director });
    }
    private Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
    }

    private void SetupHttpContext(string userId)
    {
        var httpContext = new DefaultHttpContext();

        var responseCookiesMock = new Mock<IResponseCookies>();
        responseCookiesMock.Setup(c => c.Append(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CookieOptions>()));

        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        _httpContextAccessorMock.Setup(a => a.HttpContext.Request.Cookies[USER]).Returns(userId);
        _httpContextAccessorMock.Setup(a => a.HttpContext.Response.Cookies).Returns(responseCookiesMock.Object);
    }

    private void SetupUserRoles(Mock<UserManager<User>> userManagerMock, User user, List<string> roles)
    {
        userManagerMock.Setup(m => m.GetRolesAsync(It.Is<User>(u => u.Id == user.Id)))
            .ReturnsAsync(roles);
    }

    [Test]
    public async Task SignUpAsync_ShouldCreateUser_WhenDataIsValid()
    {
        // Arrange
        var newUser = new User
        {
            Id = "new-user-id",
            Email = _createUser.Email.ToLower(),
            UserName = _createUser.Email.ToLower(),
            FirstName = _createUser.FirstName,
            LastName = _createUser.LastName,
            DocNumber = _createUser.DocNumber,
            ManagerId = _createUser.ManagerId,
        };

        var rawEmailToken = "email-confirmation-token";
        var encodedEmailToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawEmailToken));

        _userManagerMock.Setup(m => m.FindByIdAsync(_currentUserId))
            .ReturnsAsync(_currentUser);

        _userManagerMock.Setup(m => m.FindByNameAsync(_createUser.Email.ToLower()))
            .ReturnsAsync((User)null);

        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), _credentials.Password))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<User, string>((user, password) =>
            {
                user.Id = newUser.Id;
            });

        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), _createUser.Role))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync(rawEmailToken);

        _authServiceMock.Setup(a => a.GenerateTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(new Token
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
            });

        // Act
        var confirmationToken = await _userService.SignUpAsync(_credentials, _createUser);

        // Assert
        confirmationToken.Should().NotBeNullOrEmpty();
        confirmationToken.Should().Be(encodedEmailToken);

        _userManagerMock.Verify(m => m.CreateAsync(It.Is<User>(u =>
            u.Email == _createUser.Email.ToLower() &&
            u.FirstName == _createUser.FirstName &&
            u.LastName == _createUser.LastName &&
            u.DocNumber == _createUser.DocNumber &&
            u.ManagerId == _createUser.ManagerId
        ), _credentials.Password), Times.Once);

        _userManagerMock.Verify(m => m.AddToRoleAsync(It.Is<User>(u => u.Id == newUser.Id), _createUser.Role), Times.Once);
        _userManagerMock.Verify(m => m.GenerateEmailConfirmationTokenAsync(It.Is<User>(u => u.Id == newUser.Id)), Times.Once);
    }

    [Test]
    public async Task SignInAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        var credentials = new NetworkCredential("nonexistentuser@example.com", "password123");

        _userManagerMock.Setup(m => m.FindByNameAsync(credentials.UserName))
            .ReturnsAsync((User)null);

        // Act
        Func<Task> act = async () => await _userService.SignInAsync(credentials);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage($"User {credentials.UserName} not found!");

    }

    [Test]
    public async Task SignInAsync_ShouldReturnToken_WhenLoginIsSuccessful()
    {
        // Arrange
        var token = new Token
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token"
        };

        _userManagerMock.Setup(m => m.FindByNameAsync(_currentUser.UserName.ToLower()))
            .ReturnsAsync(_currentUser);

        _userManagerMock.Setup(m => m.GetLockoutEnabledAsync(_currentUser))
            .ReturnsAsync(false);

        _encryptionServiceMock.Setup(e => e.Decrypt(_credentials.Password))
            .Returns(_credentials.Password);

        _userManagerMock.Setup(m => m.CheckPasswordAsync(_currentUser, _credentials.Password))
            .ReturnsAsync(true);

        _authServiceMock.Setup(a => a.GenerateTokenAsync(_currentUser.UserName))
            .ReturnsAsync(token);

        // Act
        var result = await _userService.SignInAsync(_credentials);

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

        // Configurar o mock para remover o refresh token com sucesso
        _authServiceMock.Setup(a => a.RemoveRefreshTokenAsync(_currentUserId))
            .ReturnsAsync(true);

        // Mock para manipulação de cookies no HttpContext
        var responseCookiesMock = new Mock<IResponseCookies>();
        responseCookiesMock.Setup(c => c.Delete(It.IsAny<string>()));

        _httpContextAccessorMock.Setup(a => a.HttpContext.Response.Cookies)
            .Returns(responseCookiesMock.Object);

        // Act
        var result = await _userService.SignOutAsync();

        // Assert
        result.Should().BeTrue();

        // Verificar que o refresh token foi removido
        _authServiceMock.Verify(a => a.RemoveRefreshTokenAsync(_currentUserId), Times.Once);

        // Verificar que os cookies foram deletados
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

        _httpContextAccessorMock.Setup(a => a.HttpContext.Response.Cookies)
            .Returns(responseCookiesMock.Object);

        // Act
        var result = await _userService.SignOutAsync();

        // Assert
        result.Should().BeFalse();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error occurred during sign out.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Test]
    public async Task SignUpAsync_ShouldThrowBusinessException_WhenRoleIsInvalid()
    {
        // Arrange
        _createUser.Role = "InvalidRole";

        // Act
        Func<Task> act = async () => await _userService.SignUpAsync(_credentials, _createUser);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Couldn't create a new user! ");
    }

    [Test]
    public async Task SignUpAsync_ShouldThrowBusinessException_WhenEmailAlreadyExists()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByNameAsync(_createUser.Email.ToLower()))
            .ReturnsAsync(new User { Email = _createUser.Email.ToLower() });

        // Act
        Func<Task> act = async () => await _userService.SignUpAsync(_credentials, _createUser);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage($"Couldn't create a new user! ");
    }

    [Test]
    public async Task UpdateUserAsync_ShouldThrowException_WhenUpdateFails()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByIdAsync(_currentUserId))
            .ReturnsAsync(_currentUser);

        _userManagerMock.Setup(m => m.FindByIdAsync(_currentUserId))
            .ReturnsAsync(_existingUser);

        _userManagerMock.Setup(m => m.UpdateAsync(_existingUser))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed." }));

        // Act
        var act = async () => await _userService.UpdateUserAsync(_currentUserId, _updateUser);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Failed to update user: Update failed.");
    }

    [Test]
    public async Task SignUpAsync_ShouldThrowBusinessException_WhenUnhandledExceptionOccurs()
    {
        // Arrange
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), _credentials.Password))
            .Throws(new InvalidOperationException("An unexpected error occurred."));

        // Act
        var act = async () => await _userService.SignUpAsync(_credentials, _createUser);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Couldn't create a new user! ");
    }

    [Test]
    public async Task UpdateUserAsync_ShouldUpdateUser_WhenHierarchyIsRespected()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByIdAsync(_currentUserId))
            .ReturnsAsync(_currentUser);

        _userManagerMock.Setup(m => m.FindByIdAsync(_currentUserId))
            .ReturnsAsync(_existingUser);

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
    public async Task UpdateUserAsync_ShouldThrowException_WhenUserDoesNotHavePermission()
    {
        // Arrange
        _updateUser.Role = RoleName.Director;

        SetupHttpContext(_currentUserId);

        _userManagerMock.Setup(m => m.FindByIdAsync(_currentUserId))
            .ReturnsAsync(_currentUser);

        _userManagerMock.Setup(m => m.GetRolesAsync(It.Is<User>(u => u.Id == _currentUserId)))
            .ReturnsAsync(new List<string> { RoleName.Leader });

        // Act
        var act = async () => await _userService.UpdateUserAsync(_currentUserId, _updateUser);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage($"You do not have permission to assign the role '{_updateUser.Role}'.");
    }

    [Test]
    public async Task UpdateUserAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        var userId = "nonexistent-user-id";

        _userManagerMock.Setup(m => m.FindByIdAsync(_currentUserId))
            .ReturnsAsync(_currentUser);

        _userManagerMock.Setup(m => m.FindByIdAsync(userId))
            .ReturnsAsync((User)null);

        // Act
        var act = async () => await _userService.UpdateUserAsync(userId, _updateUser);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage($"User not found.");
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
            .ReturnsAsync((User)null);

        // Act
        var act = async () => await _userService.DeleteUserAsync(_currentUserId);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage($"User with ID {_currentUserId} not found!");
    }

    [Test]
    public async Task GetAllUsersAsync_ShouldReturnAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                Id = "1",
                Email = "user1@example.com",
                FirstName = "John",
                LastName = "Doe",
                DocNumber = "123456",
                ManagerId = "2",
                PhoneNumber = "123456789,987654321",
            },
            new User
            {
                Id = "2",
                Email = "user2@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                DocNumber = "654321",
                PhoneNumber = "111222333"
            }
        }.AsQueryable();

        var userRoles = new Dictionary<string, List<string>>
        {
            { "1", new List<string> { "Admin", "Employee" } },
            { "2", new List<string> { "Manager" } }
        };

        _userManagerMock.Setup(m => m.Users).Returns(users);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) => userRoles.ContainsKey(user.Id) ? userRoles[user.Id] : new List<string>());

        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var firstUser = result.FirstOrDefault(u => u.UserId == "1");
            firstUser.Should().NotBeNull();
            firstUser!.FirstName.Should().Be("John");
            firstUser.LastName.Should().Be("Doe");
            firstUser.Email.Should().Be("user1@example.com");
            firstUser.DocNumber.Should().Be("123456");
            firstUser.ManagerId.Should().Be("2");
            firstUser.ManagerName.Should().Be("Jane"); 
            firstUser.PhoneNumbers.Should().BeEquivalentTo(new List<string> { "123456789", "987654321" });

        var secondUser = result.FirstOrDefault(u => u.UserId == "2");
            secondUser.Should().NotBeNull();
            secondUser!.FirstName.Should().Be("Jane");
            secondUser.LastName.Should().Be("Smith");
            secondUser.Email.Should().Be("user2@example.com");
            secondUser.DocNumber.Should().Be("654321");
            secondUser.ManagerId.Should().BeNull();
            secondUser.ManagerName.Should().BeNull();
            secondUser.PhoneNumbers.Should().BeEquivalentTo(new List<string> { "111222333" });
    }
}