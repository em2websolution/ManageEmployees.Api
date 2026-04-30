using FluentAssertions;
using ManageEmployees.Api.Controllers;
using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Exceptions;
using ManageEmployees.Domain.Interfaces.Services;
using ManageEmployees.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class UsersControllerTests
{
    private Mock<IUserQueryService> _userQueryServiceMock;
    private Mock<IUserCommandService> _userCommandServiceMock;
    private UsersController _controller;

    private CreateUser _createUserRequest;
    private UpdateUser _updateUserRequest;

    [SetUp]
    public void Setup()
    {
        _userQueryServiceMock = new Mock<IUserQueryService>();
        _userCommandServiceMock = new Mock<IUserCommandService>();

        _controller = new UsersController(_userQueryServiceMock.Object, _userCommandServiceMock.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
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

    // ── CreateAsync ─────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_ShouldReturnCreated_WhenUserCreated()
    {
        _userCommandServiceMock
            .Setup(s => s.SignUpAsync(It.IsAny<CreateUser>()))
            .ReturnsAsync("User created successfully!");

        var result = await _controller.CreateAsync(_createUserRequest);

        var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
    }

    [Test]
    public async Task CreateAsync_ShouldThrow_WhenServiceThrows()
    {
        _userCommandServiceMock
            .Setup(s => s.SignUpAsync(It.IsAny<CreateUser>()))
            .ThrowsAsync(new BusinessException("Email already exists"));

        Func<Task> act = async () => await _controller.CreateAsync(_createUserRequest);

        await act.Should().ThrowAsync<BusinessException>();
    }

    // ── UpdateAsync ─────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_ShouldReturnOk_WhenUpdateSucceeds()
    {
        _userCommandServiceMock
            .Setup(s => s.UpdateUserAsync("user-123", _updateUserRequest))
            .ReturnsAsync(true);

        var result = await _controller.UpdateAsync("user-123", _updateUserRequest);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task UpdateAsync_ShouldThrow_WhenServiceThrows()
    {
        _userCommandServiceMock
            .Setup(s => s.UpdateUserAsync("user-123", _updateUserRequest))
            .ThrowsAsync(new NotFoundException("User not found"));

        Func<Task> act = async () => await _controller.UpdateAsync("user-123", _updateUserRequest);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── DeleteAsync ─────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenDeleteSucceeds()
    {
        _userCommandServiceMock.Setup(s => s.DeleteUserAsync("user-123")).ReturnsAsync(true);

        var result = await _controller.DeleteAsync("user-123");

        var noContentResult = result.Should().BeOfType<NoContentResult>().Subject;
        noContentResult.StatusCode.Should().Be(204);
    }

    [Test]
    public async Task DeleteAsync_ShouldThrow_WhenServiceThrows()
    {
        _userCommandServiceMock
            .Setup(s => s.DeleteUserAsync("user-123"))
            .ThrowsAsync(new NotFoundException("User not found"));

        Func<Task> act = async () => await _controller.DeleteAsync("user-123");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── GetAllAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_ShouldReturnOk_WithUserList()
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

        var result = await _controller.GetAllAsync();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Test]
    public async Task GetAllAsync_ShouldThrow_WhenServiceThrows()
    {
        _userQueryServiceMock
            .Setup(s => s.GetAllUsersAsync(1, 10, null, null))
            .ThrowsAsync(new Exception("Database error"));

        Func<Task> act = async () => await _controller.GetAllAsync();

        await act.Should().ThrowAsync<Exception>();
    }
}
