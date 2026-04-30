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
public class UserServiceAdditionalTests
{
    private Mock<UserManager<User>> _userManagerMock;
    private Mock<IAuthService> _authServiceMock;
    private Mock<ILogger<UserService>> _loggerMock;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private Mock<IUserRepository> _userRepositoryMock;
    private UserService _userService;
    private const string USER = "user";

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

        SetupHttpContext("default-user-id");
    }

    private static Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private void SetupHttpContext(string? userId)
    {
        var httpContext = new DefaultHttpContext();

        var responseCookiesMock = new Mock<IResponseCookies>();
        responseCookiesMock.Setup(c => c.Append(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CookieOptions>()));

        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        _httpContextAccessorMock.Setup(a => a.HttpContext!.Request.Cookies[USER]).Returns(userId);
        _httpContextAccessorMock.Setup(a => a.HttpContext!.Response.Cookies).Returns(responseCookiesMock.Object);
    }

    [Test]
    public async Task SignInAsync_ShouldThrowBusinessException_WhenPasswordIsInvalid()
    {
        // Arrange
        var user = new User { Id = "user-1", UserName = "test@test.com", Email = "test@test.com", FirstName = "Test" };
        var credentials = new NetworkCredential("test@test.com", "wrongpassword");

        _userManagerMock.Setup(m => m.FindByNameAsync(credentials.UserName))
            .ReturnsAsync(user);

        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, credentials.Password))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _userService.SignInAsync(credentials);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Invalid password!");
    }

    [Test]
    public async Task SignInAsync_ShouldThrowBusinessException_WhenUserNameIsEmpty()
    {
        // Arrange
        var user = new User { Id = "user-1", UserName = "", Email = "test@test.com", FirstName = "Test" };
        var credentials = new NetworkCredential("test@test.com", "validpassword");

        _userManagerMock.Setup(m => m.FindByNameAsync(credentials.UserName))
            .ReturnsAsync(user);

        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, credentials.Password))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _userService.SignInAsync(credentials);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("User name is missing!");
    }

    [Test]
    public async Task SignUpAsync_ShouldThrowBusinessException_WhenCreateFails()
    {
        // Arrange
        var createUser = new CreateUser
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@test.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            DocNumber = "987654321",
            Role = RoleName.Employee
        };

        _userManagerMock.Setup(m => m.FindByNameAsync(createUser.Email.ToLower()))
            .ReturnsAsync((User?)null);

        var identityErrors = new[]
        {
            new IdentityError { Code = "DuplicateEmail", Description = "Email already taken" },
            new IdentityError { Code = "PasswordTooWeak", Description = "Password too weak" }
        };

        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), createUser.Password))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        Func<Task> act = async () => await _userService.SignUpAsync(createUser);

        // Assert
        var ex = await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Couldn't create a new user!");
        ex.Which.Errors.Should().HaveCount(2);
        ex.Which.Errors[0].Code.Should().Be("DuplicateEmail");
        ex.Which.Errors[1].Code.Should().Be("PasswordTooWeak");
    }

    [Test]
    public async Task SignOutAsync_ShouldReturnFalse_WhenUserIdIsNullInCookies()
    {
        // Arrange
        SetupHttpContext(null);

        // Act
        var result = await _userService.SignOutAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task UpdateUserAsync_ShouldChangeRole_WhenNewRoleDiffers()
    {
        // Arrange
        var userId = "user-role-change";
        var existingUser = new User { Id = userId, Email = "user@test.com" };
        var updateUser = new UpdateUser
        {
            FirstName = "Updated",
            LastName = "User",
            Email = "user@test.com",
            DocNumber = "111222333",
            Role = RoleName.Administrator,
            Password = "NewPass123!",
            ConfirmPassword = "NewPass123!"
        };

        var passwordHasherMock = new Mock<IPasswordHasher<User>>();
        passwordHasherMock.Setup(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
            .Returns("hashed-password");
        _userManagerMock.Object.PasswordHasher = passwordHasherMock.Object;

        _userManagerMock.Setup(m => m.FindByIdAsync(userId))
            .ReturnsAsync(existingUser);

        _userManagerMock.Setup(m => m.GetRolesAsync(existingUser))
            .ReturnsAsync(new List<string> { RoleName.Employee });

        _userManagerMock.Setup(m => m.RemoveFromRolesAsync(existingUser, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(m => m.AddToRoleAsync(existingUser, RoleName.Administrator))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(m => m.UpdateAsync(existingUser))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.UpdateUserAsync(userId, updateUser);

        // Assert
        result.Should().BeTrue();
        _userManagerMock.Verify(m => m.RemoveFromRolesAsync(existingUser, It.Is<IEnumerable<string>>(r => r.Contains(RoleName.Employee))), Times.Once);
        _userManagerMock.Verify(m => m.AddToRoleAsync(existingUser, RoleName.Administrator), Times.Once);
    }

    [Test]
    public async Task GetAllUsersAsync_ShouldReturnPagedResult()
    {
        // Arrange
        var expectedResult = new PagedResult<UserDto>
        {
            Items = new List<UserDto>
            {
                new() { UserId = "1", FirstName = "John", LastName = "Doe", Email = "john@test.com", DocNumber = "123", Role = RoleName.Employee }
            },
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _userRepositoryMock.Setup(r => r.GetAllWithRolesAsync(1, 10, null, null))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _userService.GetAllUsersAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        _userRepositoryMock.Verify(r => r.GetAllWithRolesAsync(1, 10, null, null), Times.Once);
    }
}
