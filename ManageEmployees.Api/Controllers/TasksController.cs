using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ManageEmployees.Api.Controllers;

/// <summary>
/// Controller for task management CRUD operations.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    /// <summary>
    /// Initializes a new instance of <see cref="TasksController"/>.
    /// </summary>
    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Retrieve all tasks.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllAsync()
    {
        var tasks = await _taskService.GetAllAsync();
        return Ok(tasks);
    }

    /// <summary>
    /// Retrieve a task by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        var task = await _taskService.GetByIdAsync(id);

        if (task is null)
            return NotFound(new { Error = $"Task with ID {id} not found." });

        return Ok(task);
    }

    /// <summary>
    /// Create a new task.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateTaskRequest request)
    {
        try
        {
            request.UserId = User.FindFirstValue(ClaimTypes.UserData)!;
            var task = await _taskService.CreateAsync(request);
            return Created($"/Tasks/{task.Id}", task);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = "An error occurred while creating the task.", Details = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing task.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateTaskRequest request)
    {
        try
        {
            var task = await _taskService.UpdateAsync(id, request);
            return Ok(task);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = "An error occurred while updating the task.", Details = ex.Message });
        }
    }

    /// <summary>
    /// Delete a task by ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        try
        {
            var result = await _taskService.DeleteAsync(id);

            if (!result)
                return BadRequest("Failed to delete task!");

            return Ok(new { Message = "Task deleted successfully!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = "An error occurred while deleting the task.", Details = ex.Message });
        }
    }
}
