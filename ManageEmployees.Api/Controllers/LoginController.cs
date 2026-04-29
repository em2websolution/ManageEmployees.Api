using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ManageEmployees.Api.Controllers;

/// <summary>Authentication and user management.</summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class LoginController : ControllerBase
{
    private readonly IUserQueryService _userQueryService;
    private readonly IUserCommandService _userCommandService;

    /// <inheritdoc />
    public LoginController(IUserQueryService userQueryService, IUserCommandService userCommandService)
    {
        _userQueryService = userQueryService;
        _userCommandService = userCommandService;
    }

    /// <summary>
    /// Sign into the application.
    /// </summary>
    [HttpPost("SignIn")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SignInAsync([FromBody] SignInRequest signInRequest)
    {
        var credentials = new NetworkCredential(signInRequest.UserName, signInRequest.Password);
        var token = await _userCommandService.SignInAsync(credentials);
        return Ok(token);
    }

    /// <summary>
    /// Create a new user.
    /// </summary>
    [HttpPost("SignUp")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SignUpAsync([FromBody] CreateUser createUser)
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
    public async Task<IActionResult> UpdateUserAsync(string userId, [FromBody] UpdateUser updateUser)
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
    public async Task<IActionResult> DeleteUserAsync(string userId)
    {
        await _userCommandService.DeleteUserAsync(userId);
        return NoContent();
    }

    /// <summary>
    /// Logout from the application.
    /// </summary>
    [HttpPost("SignOut")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SignOutAsync()
    {
        var result = await _userCommandService.SignOutAsync();

        if (!result)
            return BadRequest(new { Message = "Sign out failed!" });

        return Ok(new { Message = "Sign out successful!" });
    }

    /// <summary>
    /// Retrieve all users with pagination.
    /// </summary>
    [HttpGet("ListAll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllUsersAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var users = await _userQueryService.GetAllUsersAsync(page, pageSize);
        return Ok(users);
    }
}