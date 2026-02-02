using FeeManagementService.Models;
using FeeManagementService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FeeManagementService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    /// <param name="request">Login credentials (Username, Password, SchoolId)</param>
    /// <returns>JWT token with user information</returns>
    /// <response code="200">Login successful, returns JWT token</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Invalid credentials</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for Username: {Username}, SchoolId: {SchoolId}", 
                request.Username, request.SchoolId);

            // TODO: In a real application, validate credentials against database
            // For now, this is a POC endpoint that generates tokens for any valid request
            // In production, you should:
            // 1. Validate username/password against user store
            // 2. Verify user belongs to the specified school
            // 3. Get user role from database
            // 4. Check if user is active

            // Validation
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return Problem(
                    title: "Invalid Request",
                    detail: "Username is required.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrWhiteSpace(request.SchoolId))
            {
                return Problem(
                    title: "Invalid Request",
                    detail: "SchoolId is required.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            // Validate SchoolId is a valid GUID
            if (!Guid.TryParse(request.SchoolId, out var schoolIdGuid))
            {
                return Problem(
                    title: "Invalid Request",
                    detail: "SchoolId must be a valid GUID.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            // For POC: Accept any password and generate token
            // In production, validate password here
            // For now, we'll use a simple validation (you can remove this in production)
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return Problem(
                    title: "Unauthorized",
                    detail: "Invalid credentials.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // Generate user ID (in production, get from database)
            var userId = $"user-{request.Username.GetHashCode():X}";

            // For POC: Default role is SchoolAdmin
            // In production, get role from database based on user
            var role = "SchoolAdmin";

            // Generate JWT token
            var token = _jwtTokenService.GenerateToken(
                userId: userId,
                schoolId: request.SchoolId,
                role: role,
                username: request.Username);

            var expiresAt = DateTime.UtcNow.AddMinutes(60); // Match ExpirationMinutes from config

            _logger.LogInformation(
                "Login successful for Username: {Username}, UserId: {UserId}, SchoolId: {SchoolId}",
                request.Username, userId, request.SchoolId);

            var response = new LoginResponse
            {
                Token = token,
                ExpiresAt = expiresAt,
                UserId = userId,
                SchoolId = request.SchoolId,
                Role = role
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for Username: {Username}", request.Username);
            return Problem(
                title: "Internal Server Error",
                detail: "An error occurred while processing your login request.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

