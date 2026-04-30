using FluentAssertions;
using ManageEmployees.Domain.Models;
using System.Text.Json;

namespace ManageEmployees.UnitTests;

[TestFixture]
public class ApiErrorResponseTests
{
    private static readonly string[] Field1Errors = ["Error1", "Error2"];
    private static readonly string[] InvalidError = ["Invalid"];

    [Test]
    public void Properties_ShouldBeSetAndGetCorrectly()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Field1", Field1Errors }
        };

        // Act
        var response = new ApiErrorResponse
        {
            Type = "ValidationError",
            Title = "Validation failed",
            Status = 400,
            Detail = "Some detail",
            TraceId = "trace-123",
            Errors = errors
        };

        // Assert
        response.Type.Should().Be("ValidationError");
        response.Title.Should().Be("Validation failed");
        response.Status.Should().Be(400);
        response.Detail.Should().Be("Some detail");
        response.TraceId.Should().Be("trace-123");
        response.Errors.Should().BeSameAs(errors);
    }

    [Test]
    public void Errors_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var response = new ApiErrorResponse
        {
            Type = "Error",
            Title = "Error",
            Status = 500
        };

        // Assert
        response.Errors.Should().BeNull();
    }

    [Test]
    public void JsonSerialization_ShouldUseCamelCasePropertyNames()
    {
        // Arrange
        var response = new ApiErrorResponse
        {
            Type = "NotFound",
            Title = "Resource not found",
            Status = 404,
            Detail = "Not found",
            TraceId = "abc-123",
            Errors = new Dictionary<string, string[]>
            {
                { "id", InvalidError }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        root.TryGetProperty("type", out _).Should().BeTrue();
        root.TryGetProperty("title", out _).Should().BeTrue();
        root.TryGetProperty("status", out _).Should().BeTrue();
        root.TryGetProperty("detail", out _).Should().BeTrue();
        root.TryGetProperty("traceId", out _).Should().BeTrue();
        root.TryGetProperty("errors", out _).Should().BeTrue();
    }

    [Test]
    public void JsonSerialization_ShouldExcludeErrors_WhenNull()
    {
        // Arrange
        var response = new ApiErrorResponse
        {
            Type = "InternalError",
            Title = "An error occurred",
            Status = 500,
            Errors = null
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        root.TryGetProperty("errors", out _).Should().BeFalse();
    }
}
