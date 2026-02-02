using System.Diagnostics;

namespace FeeManagementService.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = context.TraceIdentifier;

        // Add correlation ID to response headers
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        // Log request
        var schoolId = context.GetSchoolId() ?? "N/A";
        var userId = context.GetUserId() ?? "N/A";
        var path = context.Request.Path;
        var method = context.Request.Method;

        // Skip logging for health checks and swagger
        var shouldLog = !path.StartsWithSegments("/health") &&
                       !path.StartsWithSegments("/swagger") &&
                       !path.StartsWithSegments("/favicon.ico");

        if (shouldLog)
        {
            _logger.LogInformation(
                "Incoming request: {Method} {Path}, SchoolId: {SchoolId}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                method, path, schoolId, userId, correlationId);
        }

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;

            if (shouldLog)
            {
                var logLevel = statusCode >= 500 ? LogLevel.Error :
                              statusCode >= 400 ? LogLevel.Warning :
                              LogLevel.Information;

                _logger.Log(logLevel,
                    "Request completed: {Method} {Path}, StatusCode: {StatusCode}, ElapsedMs: {ElapsedMs}, SchoolId: {SchoolId}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                    method, path, statusCode, stopwatch.ElapsedMilliseconds, schoolId, userId, correlationId);
            }
        }
    }
}

