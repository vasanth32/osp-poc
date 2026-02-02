using System.ComponentModel.DataAnnotations;

namespace FeeManagementService.Models;

/// <summary>
/// Request model for creating a new fee
/// </summary>
public class CreateFeeRequest
{
    /// <summary>
    /// Title of the fee (required, max 200 characters)
    /// </summary>
    /// <example>Class Fee - Semester 1</example>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description of the fee (optional, max 2000 characters)
    /// </summary>
    /// <example>Fee for first semester classes including all subjects</example>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Fee amount (required, must be greater than 0)
    /// </summary>
    /// <example>1500.00</example>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Type of fee (required). Valid values: ActivityFee, ClassFee, CourseFee, TransportFee, LabFee, MiscFee
    /// </summary>
    /// <example>ClassFee</example>
    [Required]
    public string FeeType { get; set; } = string.Empty;

    /// <summary>
    /// Image file for direct upload (optional, not used in presigned URL workflow)
    /// </summary>
    public IFormFile? Image { get; set; }

    /// <summary>
    /// S3 URL of the uploaded image (provided by client after uploading to S3 using presigned URL)
    /// Format: https://bucket.s3.region.amazonaws.com/key
    /// </summary>
    /// <example>https://school-platform-fees-vasanth.s3.us-east-1.amazonaws.com/schools/{schoolId}/fees/{feeId}/image.jpg</example>
    [MaxLength(500)]
    public string? ImageUrl { get; set; }
}

