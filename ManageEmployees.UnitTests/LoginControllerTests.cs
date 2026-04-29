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
    private Mock<IUserQueryService> _userQueryServiceMock;
    private Mock<IUserCommandService> _userCommandServiceMock;
    private LoginController _controller;

    private Token _sampleToken;
    private SignInRequest _signInRequest;
    private CreateUser _createUserRequest;
    private UpdateUser _updateUserRequest;

    [SetUp]
    public void Setup()
    {
        _userQueryServiceMock = new Mock<IUserQueryService>();
        _userCommandServiceMock = new Mock<IUserCommandService>();

        _controller = new LoginController(_userQueryServiceMock.Object, _userCommandServiceMock.Object);
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
        _userCommandServiceMock
            .Setup(s => s.SignInAsync(It.IsAny<NetworkCredential>()))
            .ReturnsAsync(_sampleToken);

        var result = await _controller.SignInAsync(_signInRequest);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(_sampleToken);
    }

    [Test]
    public async Task SignInAsync_ShouldThrow_WhenServiceThrows()
    {
        _userCommandServiceMock
            .Setup(s => s.SignInAsync(It.IsAny<NetworkCredential>()))
            .ThrowsAsync(new NotFoundException("User not found"));

        Func<Task> act = async () => await _controller.SignInAsync(_signInRequest);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── SignUpAsync ──────────────────────────────────────────────

    [Test]
    public async Task SignUpAsync_ShouldReturnCreated_WhenUserCreated()
    {
        _userCommandServiceMock
            .Setup(s => s.SignUpAsync(It.IsAny<CreateUser>()))
            .ReturnsAsync("User created successfully!");

        var result = await _controller.SignUpAsync(_createUserRequest);

        var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Test]
    public async Task SignUpAsync_ShouldThrow_WhenServiceThrows()
    {
        _userCommandServiceMock
            .Setup(s => s.SignUpAsync(It.IsAny<CreateUser>()))
            .ThrowsAsync(new BusinessException("Email already exists"));

        Func<Task> act = async () => await _controller.SignUpAsync(_createUserRequest);

        await act.Should().ThrowAsync<BusinessException>();
    }

    // ── UpdateUserAsync ─────────────────────────────────────────

    [Test]
    public async Task UpdateUserAsync_ShouldReturnOk_WhenUpdateSucceeds()
    {
        _userCommandServiceMock
            .Setup(s => s.UpdateUserAsync("user-123", _updateUserRequest))
            .ReturnsAsync(true);

        var result = await _controller.UpdateUserAsync("user-123", _updateUserRequest);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task UpdateUserAsync_ShouldThrow_WhenServiceThrows()
    {
        _userCommandServiceMock
            .Setup(s => s.UpdateUserAsync("user-123", _updateUserRequest))
            .ThrowsAsync(new NotFoundException("User not found"));

        Func<Task> act = async () => await _controller.UpdateUserAsync("user-123", _updateUserRequest);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── DeleteUserAsync ─────────────────────────────────────────

    [Test]
    public async Task DeleteUserAsync_ShouldReturnNoContent_WhenDeleteSucceeds()
    {
        _userCommandServiceMock.Setup(s => s.DeleteUserAsync("user-123")).ReturnsAsync(true);

        var result = await _controller.DeleteUserAsync("user-123");

        var noContentResult = result.Should().BeOfType<NoContentResult>().Subject;
        noContentResult.StatusCode.Should().Be(204);
    }

    [Test]
    public async Task DeleteUserAsync_ShouldThrow_WhenServiceThrows()
    {
        _userCommandServiceMock
            .Setup(s => s.DeleteUserAsync("user-123"))
            .ThrowsAsync(new NotFoundException("User not found"));

        Func<Task> act = async () => await _controller.DeleteUserAsync("user-123");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── SignOutAsync ────────────────────────────────────────────

    [Test]
    public async Task SignOutAsync_ShouldReturnOk_WhenSignOutSucceeds()
    {
        _userCommandServiceMock.Setup(s => s.SignOutAsync()).ReturnsAsync(true);

        var result = await _controller.SignOutAsync();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task SignOutAsync_ShouldReturnBadRequest_WhenSignOutFails()
    {
        _userCommandServiceMock.Setup(s => s.SignOutAsync()).ReturnsAsync(false);

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

        var pagedResult = new PagedResult<UserDto>
        {
            Items = users,
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _userQueryServiceMock.Setup(s => s.GetAllUsersAsync(1, 10, null, null)).ReturnsAsync(pagedResult);

        var result = await _controller.GetAllUsersAsync();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Test]
    public async Task GetAllUsersAsync_ShouldThrow_WhenServiceThrows()
    {
        _userQueryServiceMock
            .Setup(s => s.GetAllUsersAsync(1, 10, null, null))
            .ThrowsAsync(new Exception("Database error"));

        Func<Task> act = async () => await _controller.GetAllUsersAsync();

        await act.Should().ThrowAsync<Exception>();
    }
}
