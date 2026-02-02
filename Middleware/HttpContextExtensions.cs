namespace FeeManagementService.Middleware;

public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the SchoolId from HttpContext.Items (set by TenantMiddleware)
    /// </summary>
    /// <param name="context">The HttpContext</param>
    /// <returns>SchoolId as string, or null if not found</returns>
    public static string? GetSchoolId(this HttpContext context)
    {
        return context.Items["SchoolId"]?.ToString();
    }

    /// <summary>
    /// Gets the SchoolId as Guid from HttpContext.Items
    /// </summary>
    /// <param name="context">The HttpContext</param>
    /// <returns>SchoolId as Guid, or null if not found or invalid</returns>
    public static Guid? GetSchoolIdAsGuid(this HttpContext context)
    {
        var schoolId = context.Items["SchoolId"]?.ToString();
        if (string.IsNullOrWhiteSpace(schoolId))
            return null;

        return Guid.TryParse(schoolId, out var guid) ? guid : null;
    }

    /// <summary>
    /// Gets the UserId from HttpContext.Items (set by TenantMiddleware)
    /// </summary>
    /// <param name="context">The HttpContext</param>
    /// <returns>UserId as string, or null if not found</returns>
    public static string? GetUserId(this HttpContext context)
    {
        return context.Items["UserId"]?.ToString();
    }

    /// <summary>
    /// Gets the Role from HttpContext.Items (set by TenantMiddleware)
    /// </summary>
    /// <param name="context">The HttpContext</param>
    /// <returns>Role as string, or null if not found</returns>
    public static string? GetRole(this HttpContext context)
    {
        return context.Items["Role"]?.ToString();
    }

    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    /// <param name="context">The HttpContext</param>
    /// <param name="role">The role to check</param>
    /// <returns>True if user has the role, false otherwise</returns>
    public static bool HasRole(this HttpContext context, string role)
    {
        var userRole = context.GetRole();
        return !string.IsNullOrWhiteSpace(userRole) && 
               userRole.Equals(role, StringComparison.OrdinalIgnoreCase);
    }
}

