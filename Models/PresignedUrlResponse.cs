namespace FeeManagementService.Models;

public class PresignedUrlResponse
{
    public string PresignedUrl { get; set; } = string.Empty;
    public string S3Key { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

