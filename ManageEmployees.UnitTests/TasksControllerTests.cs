using FluentAssertions;
using ManageEmployees.Api.Controllers;
using ManageEmployees.Domain;
using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Exceptions;
using ManageEmployees.Domain.Interfaces.Services;
using ManageEmployees.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class TasksControllerTests
{
    private Mock<ITaskQueryService> _taskQueryServiceMock;
    private Mock<ITaskCommandService> _taskCommandServiceMock;
    private TasksController _controller;

    private TaskItem _sampleTask;
    private CreateTaskRequest _createRequest;
    private UpdateTaskRequest _updateRequest;

    [SetUp]
    public void Setup()
    {
        _taskQueryServiceMock = new Mock<ITaskQueryService>();
        _taskCommandServiceMock = new Mock<ITaskCommandService>();

        _controller = new TasksController(_taskQueryServiceMock.Object, _taskCommandServiceMock.Object);

        // Set up ClaimsPrincipal with UserData claim (simulates authenticated user)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.UserData, "user-123"),
            new Claim(ClaimTypes.Name, "admin@company.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _sampleTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            Description = "Description",
            Status = TaskItemStatus.Pending,
            DueDate = DateTime.UtcNow.AddDays(7),
            UserId = "user-123",
            CreatedAt = DateTime.UtcNow
        };

        _createRequest = new CreateTaskRequest
        {
            Title = "New Task",
            Description = "New Description",
            Status = TaskItemStatus.Pending,
            DueDate = DateTime.UtcNow.AddDays(5)
        };

        _updateRequest = new UpdateTaskRequest
        {
            Title = "Updated Task",
            Description = "Updated Description",
            Status = TaskItemStatus.InProgress,
            DueDate = DateTime.UtcNow.AddDays(10)
        };
    }

    // ── GetAllAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_ShouldReturnOk_WithTaskList()
    {
        var tasks = new List<TaskItem> { _sampleTask };
        var pagedResult = new PagedResult<TaskItem>
        {
            Items = tasks,
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };
        _taskQueryServiceMock.Setup(s => s.GetAllAsync(1, 10, null, null, null, null)).ReturnsAsync(pagedResult);

        var result = await _controller.GetAllAsync();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnOk_WithEmptyList_WhenNoTasks()
    {
        var pagedResult = new PagedResult<TaskItem>
        {
            Items = new List<TaskItem>(),
            Page = 1,
            PageSize = 10,
            TotalCount = 0
        };
        _taskQueryServiceMock.Setup(s => s.GetAllAsync(1, 10, null, null, null, null)).ReturnsAsync(pagedResult);

        var result = await _controller.GetAllAsync();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        var paged = okResult.Value as PagedResult<TaskItem>;
        paged!.Items.Should().BeEmpty();
    }

    // ── GetByIdAsync ────────────────────────────────────────────

    [Test]
    public async Task GetByIdAsync_ShouldReturnOk_WhenTaskExists()
    {
        _taskQueryServiceMock.Setup(s => s.GetByIdAsync(_sampleTask.Id)).ReturnsAsync(_sampleTask);

        var result = await _controller.GetByIdAsync(_sampleTask.Id);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(_sampleTask);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        var id = Guid.NewGuid();
        _taskQueryServiceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((TaskItem?)null);

        var result = await _controller.GetByIdAsync(id);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    // ── CreateAsync ─────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_ShouldReturnCreated_WhenDataIsValid()
    {
        _taskCommandServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<CreateTaskRequest>()))
            .ReturnsAsync(_sampleTask);

        var result = await _controller.CreateAsync(_createRequest);

        var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().BeEquivalentTo(_sampleTask);
    }

    [Test]
    public async Task CreateAsync_ShouldSetUserIdFromJwtClaims()
    {
        CreateTaskRequest? capturedRequest = null;
        _taskCommandServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<CreateTaskRequest>()))
            .Callback<CreateTaskRequest>(r => capturedRequest = r)
            .ReturnsAsync(_sampleTask);

        await _controller.CreateAsync(_createRequest);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.UserId.Should().Be("user-123");
    }

    [Test]
    public async Task CreateAsync_ShouldThrow_WhenServiceThrows()
    {
        _taskCommandServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<CreateTaskRequest>()))
            .ThrowsAsync(new BusinessException("Invalid status"));

        Func<Task> act = async () => await _controller.CreateAsync(_createRequest);

        await act.Should().ThrowAsync<BusinessException>();
    }

    // ── UpdateAsync ─────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_ShouldReturnOk_WhenUpdateSucceeds()
    {
        var updatedTask = new TaskItem
        {
            Id = _sampleTask.Id,
            Title = _updateRequest.Title,
            Description = _updateRequest.Description,
            Status = _updateRequest.Status,
            DueDate = _updateRequest.DueDate,
            UserId = _sampleTask.UserId
        };

        _taskCommandServiceMock
            .Setup(s => s.UpdateAsync(_sampleTask.Id, _updateRequest))
            .ReturnsAsync(updatedTask);

        var result = await _controller.UpdateAsync(_sampleTask.Id, _updateRequest);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(updatedTask);
    }

    [Test]
    public async Task UpdateAsync_ShouldThrow_WhenServiceThrows()
    {
        _taskCommandServiceMock
            .Setup(s => s.UpdateAsync(_sampleTask.Id, _updateRequest))
            .ThrowsAsync(new NotFoundException("Task not found"));

        Func<Task> act = async () => await _controller.UpdateAsync(_sampleTask.Id, _updateRequest);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── DeleteAsync ─────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenDeleteSucceeds()
    {
        _taskCommandServiceMock.Setup(s => s.DeleteAsync(_sampleTask.Id)).ReturnsAsync(true);

        var result = await _controller.DeleteAsync(_sampleTask.Id);

        var noContentResult = result.Should().BeOfType<NoContentResult>().Subject;
        noContentResult.StatusCode.Should().Be(204);
    }

    [Test]
    public async Task DeleteAsync_ShouldThrow_WhenServiceThrows()
    {
        _taskCommandServiceMock
            .Setup(s => s.DeleteAsync(_sampleTask.Id))
            .ThrowsAsync(new NotFoundException("Task not found"));

        Func<Task> act = async () => await _controller.DeleteAsync(_sampleTask.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
