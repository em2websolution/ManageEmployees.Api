using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace ManageEmployees.Api.Controllers;

/// <summary>
/// Controller responsável por fornecer endpoints para operações relacionadas ao login.
/// </summary>
[ApiController]
[Route("[controller]")]
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
    /// Sign into the application
    /// </summary>
    /// <param name="signInRequest">Model containing necessary data to sign into the application</param>
    /// <returns>
    /// In case of success: Token is stored in the cookies and a OK Status Code
    /// In case of failure: returns BadRequest
    /// </returns>
    [HttpPost]
    [Route("SignIn")]
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
            return BadRequest(new
            {
                Error = "An error occurred while signing in.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    /// <param name="createUser">Input data containing new member data</param>
    /// <returns>
    /// In case of success: Token is stored in the cookies
    /// In case of failure: returns BadRequest
    /// </returns>
    [HttpPost]
    [Route("SignUp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SignUpAsync([FromBody] CreateUser createUser)
    {
        try
        {
            var currentUser = await _userService.GetCurrentUserAsync(User.FindFirst(ClaimTypes.UserData)?.Value!);

            if (!await _userService.CanCreateUserAsync(currentUser, createUser.Role))
                return BadRequest($"You do not have permission to create a user with the role '{createUser.Role}'.");

            var credentials = new NetworkCredential(createUser.Email.ToLower(), createUser.Password);

            var isTokenCreated = await _userService.SignUpAsync(credentials, createUser);

            if (string.IsNullOrEmpty(isTokenCreated))
                return BadRequest("User creation failed or user already exists!");

            return Ok(new { Message = "User created successfully!", ConfirmationToken = isTokenCreated });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Error = "An error occurred while creating the user.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Update user
    /// </summary>
    /// <param name="userId">Input data containing user Ida</param>
    /// <param name="updateUser">Input data containing new member data</param>
    /// <returns>
    /// In case of success: Update the user
    /// In case of failure: returns BadRequest
    /// </returns>
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
            return BadRequest(new
            {
                Error = "An error occurred while updating the user.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Delete user
    /// </summary>
    /// <param name="userId">Input data containing user Ida</param>
    /// <returns>
    /// In case of success: Deletes the user
    /// In case of failure: returns BadRequest
    /// </returns>
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
            return BadRequest(new
            {
                Error = "An error occurred while deleting the user.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Logout from the application
    /// </summary>
    /// <returns>
    /// In case of success: Token is removed from the cookies
    /// In case of failure: returns BadRequest
    /// </returns>
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
            return BadRequest(new
            {
                Error = "An error occurred while signing out.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Retrieve all users in the system.
    /// </summary>
    /// <returns>List of users</returns>
    [HttpGet("ListAll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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