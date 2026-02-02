namespace FeeManagementService.Models;

/// <summary>
/// Response model for successful login
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// School ID
    /// </summary>
    public string SchoolId { get; set; } = string.Empty;

    /// <summary>
    /// User role
    /// </summary>
    public string Role { get; set; } = string.Empty;
}

