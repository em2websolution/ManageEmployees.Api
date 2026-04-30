using ManageEmployees.Domain.Exceptions;
using ManageEmployees.Domain.Models;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace ManageEmployees.Api.Middlewares;

/// <summary>Global exception handler middleware.</summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc />
    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Processes the HTTP request and handles exceptions.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorType, title) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "NotFound", "Resource not found"),
            BusinessException { Errors.Count: > 0 } => (HttpStatusCode.BadRequest, "ValidationError", "Validation failed"),
            BusinessException => (HttpStatusCode.BadRequest, "BusinessError", "Business rule violation"),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Forbidden", "Access denied"),
            _ => (HttpStatusCode.InternalServerError, "InternalError", "An unexpected error occurred")
        };

        _logger.LogError(exception, "Unhandled exception — Type: {ErrorType}, Status: {StatusCode}", errorType, (int)statusCode);

        var response = new ApiErrorResponse
        {
            Type = errorType,
            Title = title,
            Status = (int)statusCode,
            Detail = statusCode == HttpStatusCode.InternalServerError ? null : exception.Message,
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier
        };

        if (exception is BusinessException { Errors.Count: > 0 } businessEx)
        {
            response.Errors = businessEx.Errors
                .GroupBy(e => e.Code ?? "General")
                .ToDictionary(g => g.Key, g => g.Select(e => e.Message ?? string.Empty).ToArray());
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
