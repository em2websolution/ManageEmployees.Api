using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ManageEmployees.Api.Controllers;

/// <summary>Task management CRUD operations.</summary>
/// <inheritdoc />
[ApiController]
[Route("[controller]")]
[Authorize]
public class TasksController(ITaskQueryService taskQueryService, ITaskCommandService taskCommandService) : ControllerBase
{
    /// <summary>
    /// Retrieve all tasks with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var tasks = await taskQueryService.GetAllAsync(page, pageSize, search, status, startDate, endDate);
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
        var task = await taskQueryService.GetByIdAsync(id);

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
        request.UserId = User.FindFirstValue(ClaimTypes.UserData)!;
        var task = await taskCommandService.CreateAsync(request);
        return Created($"/Tasks/{task.Id}", task);
    }

    /// <summary>
    /// Update an existing task.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateTaskRequest request)
    {
        var task = await taskCommandService.UpdateAsync(id, request);
        return Ok(task);
    }

    /// <summary>
    /// Delete a task by ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        await taskCommandService.DeleteAsync(id);
        return NoContent();
    }
}
