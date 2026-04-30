using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ManageEmployees.Api.Controllers;

/// <summary>Authentication (sign-in / sign-out).</summary>
/// <inheritdoc />
[ApiController]
[Route("[controller]")]
[Authorize]
public class LoginController(IUserCommandService userCommandService) : ControllerBase
{
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
        var token = await userCommandService.SignInAsync(credentials);
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
        var result = await userCommandService.SignOutAsync();

        if (!result)
            return BadRequest(new { Message = "Sign out failed!" });

        return Ok(new { Message = "Sign out successful!" });
    }
}