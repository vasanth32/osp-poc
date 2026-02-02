using System.Net;
using System.Text.Json;
using FeeManagementService.Middleware;

namespace FeeManagementService.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception occurred. Path: {Path}, Method: {Method}, SchoolId: {SchoolId}, UserId: {UserId}",
                context.Request.Path,
                context.Request.Method,
                context.GetSchoolId() ?? "N/A",
                context.GetUserId() ?? "N/A");

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            InvalidOperationException => (int)HttpStatusCode.Forbidden,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = GetTitle(exception),
            status = context.Response.StatusCode,
            detail = GetDetail(exception),
            instance = context.Request.Path,
            traceId = context.TraceIdentifier,
            errors = GetErrors(exception)
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var json = JsonSerializer.Serialize(problemDetails, options);
        await context.Response.WriteAsync(json);
    }

    private static string GetTitle(Exception exception)
    {
        return exception switch
        {
            ArgumentException => "Bad Request",
            UnauthorizedAccessException => "Unauthorized",
            InvalidOperationException => "Forbidden",
            KeyNotFoundException => "Not Found",
            _ => "Internal Server Error"
        };
    }

    private string GetDetail(Exception exception)
    {
        if (_environment.IsDevelopment())
        {
            return exception.Message;
        }

        return exception switch
        {
            ArgumentException => exception.Message,
            UnauthorizedAccessException => exception.Message,
            InvalidOperationException => exception.Message,
            KeyNotFoundException => exception.Message,
            _ => "An error occurred while processing your request. Please try again later."
        };
    }

    private static Dictionary<string, string[]>? GetErrors(Exception exception)
    {
        // For validation errors, you might want to extract model state errors
        // For now, return null for non-validation exceptions
        return null;
    }
}

