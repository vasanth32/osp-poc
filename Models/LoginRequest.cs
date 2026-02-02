using System.ComponentModel.DataAnnotations;

namespace FeeManagementService.Models;

/// <summary>
/// Request model for user login
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User identifier (username or email)
    /// </summary>
    /// <example>user@example.com</example>
    [Required]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User password
    /// </summary>
    /// <example>password123</example>
    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// School ID (for multi-tenant support)
    /// </summary>
    /// <example>550e8400-e29b-41d4-a716-446655440000</example>
    [Required]
    public string SchoolId { get; set; } = string.Empty;
}

