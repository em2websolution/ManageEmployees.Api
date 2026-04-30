using FluentAssertions;
using ManageEmployees.Api.Middlewares;
using ManageEmployees.Domain.Exceptions;
using ManageEmployees.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class GlobalExceptionHandlerMiddlewareTests
{
    private Mock<ILogger<GlobalExceptionHandlerMiddleware>> _loggerMock;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<ApiErrorResponse?> GetResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonSerializer.DeserializeAsync<ApiErrorResponse>(context.Response.Body, JsonOptions);
    }

    [Test]
    public async Task InvokeAsync_ShouldCallNextDelegate_WhenNoExceptionThrown()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new GlobalExceptionHandlerMiddleware(next, _loggerMock.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Test]
    public async Task InvokeAsync_ShouldReturn404_WhenNotFoundExceptionThrown()
    {
        // Arrange
        RequestDelegate next = _ => throw new NotFoundException("Item not found");
        var middleware = new GlobalExceptionHandlerMiddleware(next, _loggerMock.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(404);
        var body = await GetResponseBody(context);
        body.Should().NotBeNull();
        body!.Type.Should().Be("NotFound");
        body.Title.Should().Be("Resource not found");
        body.Status.Should().Be(404);
        body.Detail.Should().Be("Item not found");
    }

    [Test]
    public async Task InvokeAsync_ShouldReturn400BusinessError_WhenBusinessExceptionWithoutErrors()
    {
        // Arrange
        RequestDelegate next = _ => throw new BusinessException("Something went wrong");
        var middleware = new GlobalExceptionHandlerMiddleware(next, _loggerMock.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
        var body = await GetResponseBody(context);
        body.Should().NotBeNull();
        body!.Type.Should().Be("BusinessError");
        body.Title.Should().Be("Business rule violation");
        body.Status.Should().Be(400);
        body.Detail.Should().Be("Something went wrong");
        body.Errors.Should().BeNull();
    }

    [Test]
    public async Task InvokeAsync_ShouldReturn400ValidationError_WhenBusinessExceptionWithErrors()
    {
        // Arrange
        var errors = new List<Error>
        {
            new() { Code = "FieldA", Message = "FieldA is required" },
            new() { Code = "FieldA", Message = "FieldA must be at least 3 characters" },
            new() { Code = "FieldB", Message = "FieldB is invalid" }
        };
        RequestDelegate next = _ => throw new BusinessException("Validation failed", errors);
        var middleware = new GlobalExceptionHandlerMiddleware(next, _loggerMock.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
        var body = await GetResponseBody(context);
        body.Should().NotBeNull();
        body!.Type.Should().Be("ValidationError");
        body.Title.Should().Be("Validation failed");
        body.Status.Should().Be(400);
        body.Errors.Should().NotBeNull();
        body.Errors!.Should().ContainKey("FieldA");
        body.Errors["FieldA"].Should().HaveCount(2);
        body.Errors.Should().ContainKey("FieldB");
        body.Errors["FieldB"].Should().HaveCount(1);
    }

    [Test]
    public async Task InvokeAsync_ShouldReturn403_WhenUnauthorizedAccessExceptionThrown()
    {
        // Arrange
        RequestDelegate next = _ => throw new UnauthorizedAccessException("No access");
        var middleware = new GlobalExceptionHandlerMiddleware(next, _loggerMock.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(403);
        var body = await GetResponseBody(context);
        body.Should().NotBeNull();
        body!.Type.Should().Be("Forbidden");
        body.Title.Should().Be("Access denied");
        body.Status.Should().Be(403);
        body.Detail.Should().Be("No access");
    }

    [Test]
    public async Task InvokeAsync_ShouldReturn500WithNullDetail_WhenGenericExceptionThrown()
    {
        // Arrange
        RequestDelegate next = _ => throw new InvalidOperationException("Something broke");
        var middleware = new GlobalExceptionHandlerMiddleware(next, _loggerMock.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);
        var body = await GetResponseBody(context);
        body.Should().NotBeNull();
        body!.Type.Should().Be("InternalError");
        body.Title.Should().Be("An unexpected error occurred");
        body.Status.Should().Be(500);
        body.Detail.Should().BeNull();
    }

    [Test]
    public async Task InvokeAsync_ShouldLogError_WhenExceptionThrown()
    {
        // Arrange
        RequestDelegate next = _ => throw new Exception("Test error");
        var middleware = new GlobalExceptionHandlerMiddleware(next, _loggerMock.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Test]
    public async Task InvokeAsync_ShouldSetContentType_WhenExceptionThrown()
    {
        // Arrange
        RequestDelegate next = _ => throw new Exception("Error");
        var middleware = new GlobalExceptionHandlerMiddleware(next, _loggerMock.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.ContentType.Should().Be("application/problem+json");
    }
}
