using FeeManagementService.Models;

namespace FeeManagementService.Services;

public interface IFeeService
{
    Task<FeeResponse> CreateFeeAsync(CreateFeeRequest request, string schoolId, string userId);
}

