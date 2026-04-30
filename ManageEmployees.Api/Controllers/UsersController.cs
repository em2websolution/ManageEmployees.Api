using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManageEmployees.Api.Controllers;

/// <summary>User management (CRUD).</summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserQueryService _userQueryService;
    private readonly IUserCommandService _userCommandService;

    /// <inheritdoc />
    public UsersController(IUserQueryService userQueryService, IUserCommandService userCommandService)
    {
        _userQueryService = userQueryService;
        _userCommandService = userCommandService;
    }

    /// <summary>
    /// Create a new user.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateUser createUser)
    {
        var result = await _userCommandService.SignUpAsync(createUser);
        return Created(string.Empty, new { Message = result });
    }

    /// <summary>
    /// Update user.
    /// </summary>
    [HttpPut("{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(string userId, [FromBody] UpdateUser updateUser)
    {
        await _userCommandService.UpdateUserAsync(userId, updateUser);
        return Ok(new { Message = "User updated successfully!" });
    }

    /// <summary>
    /// Delete user.
    /// </summary>
    [HttpDelete("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(string userId)
    {
        await _userCommandService.DeleteUserAsync(userId);
        return NoContent();
    }

    /// <summary>
    /// Retrieve all users with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] string? role = null)
    {
        var users = await _userQueryService.GetAllUsersAsync(page, pageSize, search, role);
        return Ok(users);
    }
}
