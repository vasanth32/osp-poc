using FeeManagementService.Models;

namespace FeeManagementService.Services;

public interface IS3Service
{
    Task<PresignedUrlResponse> GeneratePresignedUrlAsync(
        string schoolId, 
        string feeId, 
        string fileName, 
        string contentType, 
        long fileSize, 
        int expirationMinutes = 10);

    Task<bool> DeleteImageAsync(string imageUrl);

    Task<string> GetImageUrlAsync(string s3Key);
}

