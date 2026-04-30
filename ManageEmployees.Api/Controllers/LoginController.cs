using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ManageEmployees.Api.Controllers;

/// <summary>Authentication (sign-in / sign-out).</summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class LoginController : ControllerBase
{
    private readonly IUserCommandService _userCommandService;

    /// <inheritdoc />
    public LoginController(IUserCommandService userCommandService)
    {
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
}