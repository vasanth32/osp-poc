using FeeManagementService.Models;

namespace FeeManagementService.Services;

public interface IJwtTokenService
{
    string GenerateToken(string userId, string schoolId, string role, string username);
}

