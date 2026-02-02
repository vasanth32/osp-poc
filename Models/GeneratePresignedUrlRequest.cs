using System.ComponentModel.DataAnnotations;

namespace FeeManagementService.Models;

/// <summary>
/// Request model for generating a presigned URL for S3 image upload
/// </summary>
public class GeneratePresignedUrlRequest
{
    /// <summary>
    /// Fee ID for which the image is being uploaded
    /// </summary>
    /// <example>fee-123</example>
    [Required]
    public string FeeId { get; set; } = string.Empty;

    /// <summary>
    /// Original filename of the image
    /// </summary>
    /// <example>class-fee-image.jpg</example>
    [Required]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type of the image. Valid values: image/jpeg, image/jpg, image/png, image/webp
    /// </summary>
    /// <example>image/jpeg</example>
    [Required]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes (must be between 1 byte and 5MB)
    /// </summary>
    /// <example>102400</example>
    [Required]
    [Range(1, 5 * 1024 * 1024, ErrorMessage = "File size must be between 1 byte and 5MB")]
    public long FileSize { get; set; }
}

