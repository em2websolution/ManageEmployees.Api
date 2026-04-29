using FluentAssertions;
using ManageEmployees.Domain;
using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Exceptions;
using ManageEmployees.Domain.Interfaces.Repositories;
using ManageEmployees.Domain.Models;
using ManageEmployees.Services.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class TaskServiceTests
{
    private Mock<ITaskRepository> _taskRepositoryMock;
    private Mock<ILogger<TaskService>> _loggerMock;
    private TaskService _taskService;

    private TaskItem _existingTask;
    private CreateTaskRequest _createRequest;
    private UpdateTaskRequest _updateRequest;

    [SetUp]
    public void Setup()
    {
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _loggerMock = new Mock<ILogger<TaskService>>();
        _taskService = new TaskService(_taskRepositoryMock.Object, _loggerMock.Object);

        _existingTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Existing Task",
            Description = "Description",
            Status = TaskItemStatus.Pending,
            DueDate = DateTime.UtcNow.AddDays(7),
            UserId = "user-1",
            CreatedAt = DateTime.UtcNow
        };

        _createRequest = new CreateTaskRequest
        {
            Title = "New Task",
            Description = "New Description",
            Status = TaskItemStatus.Pending,
            DueDate = DateTime.UtcNow.AddDays(5),
            UserId = "user-1"
        };

        _updateRequest = new UpdateTaskRequest
        {
            Title = "Updated Task",
            Description = "Updated Description",
            Status = TaskItemStatus.InProgress,
            DueDate = DateTime.UtcNow.AddDays(10)
        };
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllTasks()
    {
        // Arrange
        var tasks = new List<TaskItem> { _existingTask };
        var pagedResult = new PagedResult<TaskItem>
        {
            Items = tasks,
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _taskRepositoryMock.Setup(r => r.GetAllAsync(1, 10, null, null, null, null)).ReturnsAsync(pagedResult);

        // Act
        var result = await _taskService.GetAllAsync(1, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Existing Task");
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnTask_WhenExists()
    {
        // Arrange
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(_existingTask.Id)).ReturnsAsync(_existingTask);

        // Act
        var result = await _taskService.GetByIdAsync(_existingTask.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(_existingTask.Id);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((TaskItem?)null);

        // Act
        var result = await _taskService.GetByIdAsync(id);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task CreateAsync_ShouldCreateTask_WhenDataIsValid()
    {
        // Arrange
        _taskRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);

        // Act
        var result = await _taskService.CreateAsync(_createRequest);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(_createRequest.Title);
        result.Description.Should().Be(_createRequest.Description);
        result.Status.Should().Be(_createRequest.Status);
        result.UserId.Should().Be(_createRequest.UserId);

        _taskRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<TaskItem>()), Times.Once);
    }

    [Test]
    public void CreateAsync_ShouldThrowBusinessException_WhenStatusIsInvalid()
    {
        // Arrange
        _createRequest.Status = "InvalidStatus";

        // Act
        Func<Task> act = async () => await _taskService.CreateAsync(_createRequest);

        // Assert
        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Invalid status*");
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateTask_WhenExists()
    {
        // Arrange
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(_existingTask.Id)).ReturnsAsync(_existingTask);
        _taskRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);

        // Act
        var result = await _taskService.UpdateAsync(_existingTask.Id, _updateRequest);

        // Assert
        result.Title.Should().Be(_updateRequest.Title);
        result.Description.Should().Be(_updateRequest.Description);
        result.Status.Should().Be(_updateRequest.Status);

        _taskRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>()), Times.Once);
    }

    [Test]
    public void UpdateAsync_ShouldThrowNotFoundException_WhenTaskNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((TaskItem?)null);

        // Act
        Func<Task> act = async () => await _taskService.UpdateAsync(id, _updateRequest);

        // Assert
        act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Task with ID {id} not found.");
    }

    [Test]
    public void UpdateAsync_ShouldThrowBusinessException_WhenStatusIsInvalid()
    {
        // Arrange
        _updateRequest.Status = "InvalidStatus";

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(_existingTask.Id)).ReturnsAsync(_existingTask);

        // Act
        Func<Task> act = async () => await _taskService.UpdateAsync(_existingTask.Id, _updateRequest);

        // Assert
        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Invalid status*");
    }

    [Test]
    public async Task DeleteAsync_ShouldDeleteTask_WhenExists()
    {
        // Arrange
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(_existingTask.Id)).ReturnsAsync(_existingTask);
        _taskRepositoryMock.Setup(r => r.DeleteAsync(_existingTask.Id)).ReturnsAsync(true);

        // Act
        var result = await _taskService.DeleteAsync(_existingTask.Id);

        // Assert
        result.Should().BeTrue();
        _taskRepositoryMock.Verify(r => r.DeleteAsync(_existingTask.Id), Times.Once);
    }

    [Test]
    public void DeleteAsync_ShouldThrowNotFoundException_WhenTaskNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((TaskItem?)null);

        // Act
        Func<Task> act = async () => await _taskService.DeleteAsync(id);

        // Assert
        act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Task with ID {id} not found.");
    }
}
