using FluentValidation;
using FeeManagementService.Models;
using System.Text.RegularExpressions;

namespace FeeManagementService.Validators;

public class CreateFeeRequestValidator : AbstractValidator<CreateFeeRequest>
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB in bytes

    public CreateFeeRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MaximumLength(200)
            .WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Description must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.FeeType)
            .NotEmpty()
            .WithMessage("FeeType is required.")
            .Must(BeValidFeeType)
            .WithMessage("FeeType must be a valid value: ActivityFee, ClassFee, CourseFee, TransportFee, LabFee, or MiscFee.");

        RuleFor(x => x.Image)
            .Must(HaveValidFileSize)
            .WithMessage("Image file size must not exceed 5MB.")
            .When(x => x.Image != null);

        RuleFor(x => x.Image)
            .Must(HaveValidExtension)
            .WithMessage("Image must be a valid image format (JPG, JPEG, PNG, or WebP).")
            .When(x => x.Image != null);

        RuleFor(x => x.Image)
            .Must(BeValidImageFormat)
            .WithMessage("Image must be a valid image file.")
            .When(x => x.Image != null);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500)
            .WithMessage("ImageUrl must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));

        RuleFor(x => x.ImageUrl)
            .Must(BeValidS3Url)
            .WithMessage("ImageUrl must be a valid S3 URL format (e.g., https://bucket.s3.region.amazonaws.com/key).")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));
    }

    private bool BeValidFeeType(string feeType)
    {
        return Enum.TryParse<FeeType>(feeType, ignoreCase: true, out _);
    }

    private bool HaveValidFileSize(IFormFile? file)
    {
        if (file == null)
            return true;

        return file.Length <= MaxFileSize;
    }

    private bool HaveValidExtension(IFormFile? file)
    {
        if (file == null)
            return true;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }

    private bool BeValidImageFormat(IFormFile? file)
    {
        if (file == null)
            return true;

        // Check content type
        var allowedContentTypes = new[]
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/webp"
        };

        if (!string.IsNullOrEmpty(file.ContentType) && 
            allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return true;
        }

        // Also check file extension as fallback
        return HaveValidExtension(file);
    }

    private bool BeValidS3Url(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        // S3 URL pattern: https://bucket.s3.region.amazonaws.com/key
        var s3UrlPattern = new Regex(
            @"^https?://[^/]+\.s3[^/]*\.amazonaws\.com/.+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        return s3UrlPattern.IsMatch(url);
    }
}

