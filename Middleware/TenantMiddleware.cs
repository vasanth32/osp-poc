using System.Security.Claims;

namespace FeeManagementService.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip middleware for authentication endpoints (login, register, etc.)
        if (context.Request.Path.StartsWithSegments("/api/auth") ||
            context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        // Check if user is authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("Unauthenticated request to protected endpoint: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized: Authentication required.");
            return;
        }

        try
        {
            // Extract SchoolId from JWT claims (try both "SchoolId" and "TenantId")
            var schoolIdClaim = context.User.FindFirst("SchoolId") 
                ?? context.User.FindFirst("TenantId")
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier); // Fallback to NameIdentifier if custom claims not found

            if (schoolIdClaim == null || string.IsNullOrWhiteSpace(schoolIdClaim.Value))
            {
                _logger.LogWarning("SchoolId not found in JWT claims for user: {User}", context.User.Identity?.Name ?? "Unknown");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: SchoolId not found in token.");
                return;
            }

            // Extract UserId from JWT claims
            var userIdClaim = context.User.FindFirst("UserId")
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirst(ClaimTypes.Name);

            // Extract Role from JWT claims
            var roleClaim = context.User.FindFirst("Role")
                ?? context.User.FindFirst(ClaimTypes.Role);

            // Store in HttpContext.Items for easy access throughout the request
            context.Items["SchoolId"] = schoolIdClaim.Value;
            
            if (userIdClaim != null && !string.IsNullOrWhiteSpace(userIdClaim.Value))
            {
                context.Items["UserId"] = userIdClaim.Value;
            }

            if (roleClaim != null && !string.IsNullOrWhiteSpace(roleClaim.Value))
            {
                context.Items["Role"] = roleClaim.Value;
            }

            _logger.LogDebug(
                "Extracted tenant info - SchoolId: {SchoolId}, UserId: {UserId}, Role: {Role}",
                schoolIdClaim?.Value ?? "N/A",
                userIdClaim?.Value ?? "N/A",
                roleClaim?.Value ?? "N/A");

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TenantMiddleware while processing request: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Internal server error occurred.");
        }
    }
}

