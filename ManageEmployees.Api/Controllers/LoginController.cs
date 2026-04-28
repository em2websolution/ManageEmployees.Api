using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ManageEmployees.Api.Controllers;

/// <summary>
/// Controller for authentication and user management operations.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class LoginController : ControllerBase
{
    private readonly IUserService _userService;

    /// <summary>
    /// Initializes a new instance of <see cref="LoginController"/>.
    /// </summary>
    public LoginController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Sign into the application.
    /// </summary>
    [HttpPost]
    [Route("SignIn")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SignInAsync([FromBody] SignInRequest signInRequest)
    {
        try
        {
            var credentials = new NetworkCredential(signInRequest.UserName, signInRequest.Password);
            var token = await _userService.SignInAsync(credentials);

            if (token == null)
                return BadRequest();

            return Ok(token);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = "An error occurred while signing in.", Details = ex.Message });
        }
    }

    /// <summary>
    /// Create a new user.
    /// </summary>
    [HttpPost]
    [Route("SignUp")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SignUpAsync([FromBody] CreateUser createUser)
    {
        try
        {
            var result = await _userService.SignUpAsync(createUser);

            if (string.IsNullOrEmpty(result))
                return BadRequest("User creation failed or user already exists!");

            return Ok(new { Message = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = "An error occurred while creating the user.", Details = ex.Message });
        }
    }

    /// <summary>
    /// Update user.
    /// </summary>
    [HttpPut]
    [Route("{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUserAsync(string userId, [FromBody] UpdateUser updateUser)
    {
        try
        {
            var result = await _userService.UpdateUserAsync(userId, updateUser);

            if (!result)
                return BadRequest("Failed to update user!");

            return Ok(new { Message = "User updated successfully!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = "An error occurred while updating the user.", Details = ex.Message });
        }
    }

    /// <summary>
    /// Delete user.
    /// </summary>
    [HttpDelete]
    [Route("{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteUserAsync(string userId)
    {
        try
        {
            var result = await _userService.DeleteUserAsync(userId);

            if (!result)
                return BadRequest("Failed to delete user!");

            return Ok(new { Message = "User deleted successfully!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = "An error occurred while deleting the user.", Details = ex.Message });
        }
    }

    /// <summary>
    /// Logout from the application.
    /// </summary>
    [HttpPost]
    [Route("SignOut")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SignOutAsync()
    {
        try
        {
            var result = await _userService.SignOutAsync();

            if (!result)
                return BadRequest("Sign out failed!");

            return Ok(new { Message = "Sign out successful!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = "An error occurred while signing out.", Details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieve all users in the system.
    /// </summary>
    [HttpGet("ListAll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllUsersAsync()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}