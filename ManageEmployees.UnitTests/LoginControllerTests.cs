using FluentAssertions;
using ManageEmployees.Api.Controllers;
using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Exceptions;
using ManageEmployees.Domain.Interfaces.Services;
using ManageEmployees.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class LoginControllerTests
{
    private Mock<IUserService> _userServiceMock;
    private LoginController _controller;

    private Token _sampleToken;
    private SignInRequest _signInRequest;
    private CreateUser _createUserRequest;
    private UpdateUser _updateUserRequest;

    [SetUp]
    public void Setup()
    {
        _userServiceMock = new Mock<IUserService>();

        _controller = new LoginController(_userServiceMock.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        _sampleToken = new Token
        {
            AccessToken = "jwt-access-token",
            RefreshToken = "jwt-refresh-token",
            Role = "Administrator",
            FirstName = "Admin",
            UserId = "user-123"
        };

        _signInRequest = new SignInRequest
        {
            UserName = "admin@company.com",
            Password = "Admin123!"
        };

        _createUserRequest = new CreateUser
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@company.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!",
            Role = "Employee"
        };

        _updateUserRequest = new UpdateUser
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@company.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!",
            Role = "Employee"
        };
    }

    // ── SignInAsync ──────────────────────────────────────────────

    [Test]
    public async Task SignInAsync_ShouldReturnOk_WhenCredentialsAreValid()
    {
        _userServiceMock
            .Setup(s => s.SignInAsync(It.IsAny<NetworkCredential>()))
            .ReturnsAsync(_sampleToken);

        var result = await _controller.SignInAsync(_signInRequest);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(_sampleToken);
    }

    [Test]
    public async Task SignInAsync_ShouldReturnBadRequest_WhenTokenIsNull()
    {
        _userServiceMock
            .Setup(s => s.SignInAsync(It.IsAny<NetworkCredential>()))
            .ReturnsAsync((Token?)null!);

        var result = await _controller.SignInAsync(_signInRequest);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Test]
    public async Task SignInAsync_ShouldReturnBadRequest_WhenServiceThrows()
    {
        _userServiceMock
            .Setup(s => s.SignInAsync(It.IsAny<NetworkCredential>()))
            .ThrowsAsync(new BusinessException("Invalid credentials"));

        var result = await _controller.SignInAsync(_signInRequest);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }

    // ── SignUpAsync ──────────────────────────────────────────────

    [Test]
    public async Task SignUpAsync_ShouldReturnOk_WhenUserCreated()
    {
        _userServiceMock
            .Setup(s => s.SignUpAsync(It.IsAny<CreateUser>()))
            .ReturnsAsync("User created successfully!");

        var result = await _controller.SignUpAsync(_createUserRequest);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task SignUpAsync_ShouldReturnBadRequest_WhenResultIsEmpty()
    {
        _userServiceMock
            .Setup(s => s.SignUpAsync(It.IsAny<CreateUser>()))
            .ReturnsAsync(string.Empty);

        var result = await _controller.SignUpAsync(_createUserRequest);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task SignUpAsync_ShouldReturnBadRequest_WhenServiceThrows()
    {
        _userServiceMock
            .Setup(s => s.SignUpAsync(It.IsAny<CreateUser>()))
            .ThrowsAsync(new BusinessException("Email already exists"));

        var result = await _controller.SignUpAsync(_createUserRequest);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }

    // ── UpdateUserAsync ─────────────────────────────────────────

    [Test]
    public async Task UpdateUserAsync_ShouldReturnOk_WhenUpdateSucceeds()
    {
        _userServiceMock
            .Setup(s => s.UpdateUserAsync("user-123", _updateUserRequest))
            .ReturnsAsync(true);

        var result = await _controller.UpdateUserAsync("user-123", _updateUserRequest);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task UpdateUserAsync_ShouldReturnBadRequest_WhenUpdateFails()
    {
        _userServiceMock
            .Setup(s => s.UpdateUserAsync("user-123", _updateUserRequest))
            .ReturnsAsync(false);

        var result = await _controller.UpdateUserAsync("user-123", _updateUserRequest);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task UpdateUserAsync_ShouldReturnBadRequest_WhenServiceThrows()
    {
        _userServiceMock
            .Setup(s => s.UpdateUserAsync("user-123", _updateUserRequest))
            .ThrowsAsync(new BusinessException("User not found"));

        var result = await _controller.UpdateUserAsync("user-123", _updateUserRequest);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }

    // ── DeleteUserAsync ─────────────────────────────────────────

    [Test]
    public async Task DeleteUserAsync_ShouldReturnOk_WhenDeleteSucceeds()
    {
        _userServiceMock.Setup(s => s.DeleteUserAsync("user-123")).ReturnsAsync(true);

        var result = await _controller.DeleteUserAsync("user-123");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task DeleteUserAsync_ShouldReturnBadRequest_WhenDeleteFails()
    {
        _userServiceMock.Setup(s => s.DeleteUserAsync("user-123")).ReturnsAsync(false);

        var result = await _controller.DeleteUserAsync("user-123");

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task DeleteUserAsync_ShouldReturnBadRequest_WhenServiceThrows()
    {
        _userServiceMock
            .Setup(s => s.DeleteUserAsync("user-123"))
            .ThrowsAsync(new BusinessException("User not found"));

        var result = await _controller.DeleteUserAsync("user-123");

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }

    // ── SignOutAsync ────────────────────────────────────────────

    [Test]
    public async Task SignOutAsync_ShouldReturnOk_WhenSignOutSucceeds()
    {
        _userServiceMock.Setup(s => s.SignOutAsync()).ReturnsAsync(true);

        var result = await _controller.SignOutAsync();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task SignOutAsync_ShouldReturnBadRequest_WhenSignOutFails()
    {
        _userServiceMock.Setup(s => s.SignOutAsync()).ReturnsAsync(false);

        var result = await _controller.SignOutAsync();

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }

    // ── GetAllUsersAsync ────────────────────────────────────────

    [Test]
    public async Task GetAllUsersAsync_ShouldReturnOk_WithUserList()
    {
        var users = new List<UserDto>
        {
            new UserDto
            {
                UserId = "user-1",
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@company.com",
                Role = "Administrator"
            }
        };

        _userServiceMock.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(users);

        var result = await _controller.GetAllUsersAsync();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(users);
    }

    [Test]
    public async Task GetAllUsersAsync_ShouldReturnBadRequest_WhenServiceThrows()
    {
        _userServiceMock
            .Setup(s => s.GetAllUsersAsync())
            .ThrowsAsync(new Exception("Database error"));

        var result = await _controller.GetAllUsersAsync();

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }
}
