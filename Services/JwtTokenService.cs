using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FeeManagementService.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FeeManagementService.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(
        IOptions<JwtSettings> jwtSettings,
        ILogger<JwtTokenService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public string GenerateToken(string userId, string schoolId, string role, string username)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim("UserId", userId),
            new Claim("SchoolId", schoolId),
            new Claim(ClaimTypes.Role, role), // Use ClaimTypes.Role for [Authorize(Roles = "...")] to work
            new Claim("Role", role), // Keep custom claim for backward compatibility
            new Claim(ClaimTypes.Name, username)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        _logger.LogInformation(
            "JWT token generated for UserId: {UserId}, SchoolId: {SchoolId}, Role: {Role}",
            userId, schoolId, role);

        return tokenString;
    }
}

