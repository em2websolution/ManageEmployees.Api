using FluentAssertions;
using ManageEmployees.Api.Controllers;
using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Exceptions;
using ManageEmployees.Domain.Interfaces.Services;
using ManageEmployees.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class LoginControllerTests
{
    private Mock<IUserCommandService> _userCommandServiceMock;
    private LoginController _controller;

    private Token _sampleToken;
    private SignInRequest _signInRequest;

    [SetUp]
    public void Setup()
    {
        _userCommandServiceMock = new Mock<IUserCommandService>();

        _controller = new LoginController(_userCommandServiceMock.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        _sampleToken = new Token
        {
            AccessToken = "jwt-access-token",
            RefreshToken = "jwt-refresh-token",
            Role = "Administrator",
            FirstName = "Admin",
            UserId = "user-123"
        };

        _signInRequest = new SignInRequest
        {
            UserName = "admin@company.com",
            Password = "Admin123!"
        };
    }

    // ── SignInAsync ──────────────────────────────────────────────

    [Test]
    public async Task SignInAsync_ShouldReturnOk_WhenCredentialsAreValid()
    {
        _userCommandServiceMock
            .Setup(s => s.SignInAsync(It.IsAny<NetworkCredential>()))
            .ReturnsAsync(_sampleToken);

        var result = await _controller.SignInAsync(_signInRequest);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(_sampleToken);
    }

    [Test]
    public async Task SignInAsync_ShouldThrow_WhenServiceThrows()
    {
        _userCommandServiceMock
            .Setup(s => s.SignInAsync(It.IsAny<NetworkCredential>()))
            .ThrowsAsync(new NotFoundException("User not found"));

        Func<Task> act = async () => await _controller.SignInAsync(_signInRequest);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── SignOutAsync ────────────────────────────────────────────

    [Test]
    public async Task SignOutAsync_ShouldReturnOk_WhenSignOutSucceeds()
    {
        _userCommandServiceMock.Setup(s => s.SignOutAsync()).ReturnsAsync(true);

        var result = await _controller.SignOutAsync();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task SignOutAsync_ShouldReturnBadRequest_WhenSignOutFails()
    {
        _userCommandServiceMock.Setup(s => s.SignOutAsync()).ReturnsAsync(false);

        var result = await _controller.SignOutAsync();

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }
}
