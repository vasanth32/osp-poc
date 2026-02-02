using FeeManagementService.Data;
using FeeManagementService.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace FeeManagementService.Services;

public class FeeService : IFeeService
{
    private readonly FeeDbContext _context;
    private readonly IS3Service _s3Service;
    private readonly ILogger<FeeService> _logger;
    private static readonly Regex S3UrlPattern = new(
        @"^https?://[^/]+\.s3[^/]*\.amazonaws\.com/.+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public FeeService(
        FeeDbContext context,
        IS3Service s3Service,
        ILogger<FeeService> logger)
    {
        _context = context;
        _s3Service = s3Service;
        _logger = logger;
    }

    public async Task<FeeResponse> CreateFeeAsync(CreateFeeRequest request, string schoolId, string userId)
    {
        try
        {
            _logger.LogInformation(
                "Creating fee: Title={Title}, SchoolId={SchoolId}, UserId={UserId}",
                request.Title, schoolId, userId);

            // Validate SchoolId and UserId
            if (string.IsNullOrWhiteSpace(schoolId))
            {
                throw new ArgumentException("SchoolId cannot be empty.", nameof(schoolId));
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("UserId cannot be empty.", nameof(userId));
            }

            // Validate SchoolId is a valid Guid
            if (!Guid.TryParse(schoolId, out var schoolIdGuid))
            {
                throw new ArgumentException("SchoolId must be a valid GUID.", nameof(schoolId));
            }

            // Validate ImageUrl if provided (should be a valid S3 URL format)
            if (!string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                if (!IsValidS3Url(request.ImageUrl))
                {
                    _logger.LogWarning(
                        "Invalid S3 URL format provided: {ImageUrl}",
                        request.ImageUrl);
                    throw new ArgumentException(
                        "ImageUrl must be a valid S3 URL format (e.g., https://bucket.s3.region.amazonaws.com/key).",
                        nameof(request.ImageUrl));
                }
            }

            // Parse FeeType enum
            if (!Enum.TryParse<FeeType>(request.FeeType, ignoreCase: true, out var feeType))
            {
                throw new ArgumentException(
                    $"Invalid FeeType: {request.FeeType}. Must be one of: {string.Join(", ", Enum.GetNames<FeeType>())}",
                    nameof(request.FeeType));
            }

            // Create Fee entity
            var fee = new Fee
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolIdGuid,
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) 
                    ? null 
                    : request.Description.Trim(),
                Amount = request.Amount,
                FeeType = feeType,
                ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) 
                    ? null 
                    : request.ImageUrl.Trim(),
                Status = FeeStatus.Active,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            // Save to database
            _context.Fees.Add(fee);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Fee created successfully: FeeId={FeeId}, SchoolId={SchoolId}, Title={Title}",
                fee.Id, schoolId, fee.Title);

            // Map to FeeResponse
            return fee.ToResponse();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex,
                "Database error while creating fee. SchoolId={SchoolId}, UserId={UserId}",
                schoolId, userId);
            
            // Check for unique constraint violations or other DB errors
            if (ex.InnerException?.Message.Contains("UNIQUE") == true)
            {
                throw new InvalidOperationException("A fee with similar details already exists.", ex);
            }
            
            throw new InvalidOperationException("Failed to create fee due to database error.", ex);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex,
                "Invalid argument while creating fee. SchoolId={SchoolId}, UserId={UserId}",
                schoolId, userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while creating fee. SchoolId={SchoolId}, UserId={UserId}",
                schoolId, userId);
            throw new Exception("An unexpected error occurred while creating the fee.", ex);
        }
    }

    /// <summary>
    /// Validates if the provided URL is a valid S3 URL format
    /// </summary>
    /// <param name="url">The URL to validate</param>
    /// <returns>True if valid S3 URL format, false otherwise</returns>
    private static bool IsValidS3Url(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        // Check if it matches S3 URL pattern: https://bucket.s3.region.amazonaws.com/key
        return S3UrlPattern.IsMatch(url);
    }
}

