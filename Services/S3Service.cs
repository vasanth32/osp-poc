using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using FeeManagementService.Configuration;
using FeeManagementService.Models;
using Microsoft.Extensions.Options;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace FeeManagementService.Services;

public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsS3Settings _settings;
    private readonly ILogger<S3Service> _logger;
    private readonly ITelemetryService _telemetryService;
    private readonly TelemetryClient? _telemetryClient;
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedContentTypes = 
    { 
        "image/jpeg", 
        "image/jpg", 
        "image/png", 
        "image/webp" 
    };

    public S3Service(
        IAmazonS3 s3Client,
        IOptions<AwsS3Settings> settings,
        ILogger<S3Service> logger,
        ITelemetryService telemetryService,
        TelemetryClient? telemetryClient = null)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
        _logger = logger;
        _telemetryService = telemetryService;
        _telemetryClient = telemetryClient;
    }

    public async Task<PresignedUrlResponse> GeneratePresignedUrlAsync(
        string schoolId, 
        string feeId, 
        string fileName, 
        string contentType, 
        long fileSize, 
        int expirationMinutes = 10)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var dependencyTelemetry = _telemetryClient != null ? new DependencyTelemetry
        {
            Type = "AWS S3",
            Name = "S3:GeneratePresignedUrl",
            Data = $"Bucket: {_settings.BucketName}, File: {fileName}",
            Target = $"s3.amazonaws.com/{_settings.BucketName}",
            Success = false
        } : null;
        
        var startTime = DateTimeOffset.UtcNow;
        bool success = false;
        
        try
        {
            // Validate input parameters
            if (string.IsNullOrWhiteSpace(schoolId))
                throw new ArgumentException("SchoolId cannot be empty.", nameof(schoolId));

            if (string.IsNullOrWhiteSpace(feeId))
                throw new ArgumentException("FeeId cannot be empty.", nameof(feeId));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("FileName cannot be empty.", nameof(fileName));

            if (fileSize <= 0 || fileSize > MaxFileSize)
                throw new ArgumentException($"File size must be between 1 byte and {MaxFileSize} bytes (5MB).", nameof(fileSize));

            // Validate file extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new ArgumentException($"File extension must be one of: {string.Join(", ", AllowedExtensions)}", nameof(fileName));

            // Validate content type
            if (string.IsNullOrWhiteSpace(contentType) || 
                !AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
                throw new ArgumentException($"Content type must be one of: {string.Join(", ", AllowedContentTypes)}", nameof(contentType));

            // Generate unique filename: {feeId}_{timestamp}_{Guid}.{extension}
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var uniqueFileName = $"{feeId}_{timestamp}_{uniqueId}{extension}";

            // S3 key: schools/{schoolId}/fees/{feeId}/{filename}
            var s3Key = $"schools/{schoolId}/fees/{feeId}/{uniqueFileName}";

            // Create GetPreSignedUrlRequest
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _settings.BucketName,
                Key = s3Key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                ContentType = contentType,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
            };

            // Generate presigned URL (synchronous method, wrapped in Task for async compatibility)
            var presignedUrl = await Task.Run(() => _s3Client.GetPreSignedURL(request));

            // Construct final image URL: https://{bucket}.s3.{region}.amazonaws.com/{key}
            var imageUrl = $"https://{_settings.BucketName}.s3.{_settings.Region}.amazonaws.com/{s3Key}";

            var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
            
            stopwatch.Stop();
            success = true;

            // Track S3 upload telemetry
            _telemetryService.TrackS3Upload(
                schoolId,
                fileName,
                fileSize,
                success,
                stopwatch.Elapsed);

            // Track dependency if Application Insights is configured
            if (dependencyTelemetry != null)
            {
                dependencyTelemetry.Success = true;
                dependencyTelemetry.Duration = stopwatch.Elapsed;
                dependencyTelemetry.Properties.Add("SchoolId", schoolId);
                dependencyTelemetry.Properties.Add("FileName", fileName);
                dependencyTelemetry.Properties.Add("FileSize", fileSize.ToString());
                _telemetryClient!.TrackDependency(dependencyTelemetry);
            }

            _logger.LogInformation(
                "Generated presigned URL for S3 key: {S3Key}, Expires at: {ExpiresAt}, Duration: {Duration}ms",
                s3Key, expiresAt, stopwatch.ElapsedMilliseconds);

            return new PresignedUrlResponse
            {
                PresignedUrl = presignedUrl,
                S3Key = s3Key,
                ImageUrl = imageUrl,
                ExpiresAt = expiresAt
            };
        }
        catch (AmazonS3Exception ex)
        {
            stopwatch.Stop();
            success = false;
            
            // Track failed upload
            _telemetryService.TrackS3Upload(
                schoolId,
                fileName,
                fileSize,
                success,
                stopwatch.Elapsed);
            
            if (dependencyTelemetry != null)
            {
                dependencyTelemetry.Success = false;
                dependencyTelemetry.Duration = stopwatch.Elapsed;
                dependencyTelemetry.Properties.Add("Error", ex.Message);
                _telemetryClient!.TrackDependency(dependencyTelemetry);
            }
            
            _telemetryService.TrackException(ex, new Dictionary<string, string>
            {
                { "SchoolId", schoolId },
                { "FileName", fileName },
                { "Operation", "GeneratePresignedUrl" },
                { "ExceptionType", "AmazonS3Exception" }
            });
            
            _logger.LogError(ex, "AWS S3 error while generating presigned URL. SchoolId: {SchoolId}, FeeId: {FeeId}", 
                schoolId, feeId);
            throw;
        }
        catch (ArgumentException ex)
        {
            stopwatch.Stop();
            success = false;
            
            _telemetryService.TrackS3Upload(
                schoolId,
                fileName,
                fileSize,
                success,
                stopwatch.Elapsed);
            
            _telemetryService.TrackException(ex, new Dictionary<string, string>
            {
                { "SchoolId", schoolId },
                { "FileName", fileName },
                { "Operation", "GeneratePresignedUrl" },
                { "ExceptionType", "ArgumentException" }
            });
            
            _logger.LogWarning(ex, "Invalid parameters for presigned URL generation. SchoolId: {SchoolId}, FeeId: {FeeId}", 
                schoolId, feeId);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            success = false;
            
            _telemetryService.TrackS3Upload(
                schoolId,
                fileName,
                fileSize,
                success,
                stopwatch.Elapsed);
            
            _telemetryService.TrackException(ex, new Dictionary<string, string>
            {
                { "SchoolId", schoolId },
                { "FileName", fileName },
                { "Operation", "GeneratePresignedUrl" },
                { "ExceptionType", ex.GetType().Name }
            });
            
            _logger.LogError(ex, "Unexpected error while generating presigned URL. SchoolId: {SchoolId}, FeeId: {FeeId}", 
                schoolId, feeId);
            throw new Exception("Failed to generate presigned URL.", ex);
        }
    }

    public async Task<bool> DeleteImageAsync(string imageUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                _logger.LogWarning("ImageUrl is null or empty for delete operation.");
                return false;
            }

            // Extract S3 key from URL
            // URL format: https://{bucket}.s3.{region}.amazonaws.com/{key}
            var uri = new Uri(imageUrl);
            var s3Key = uri.AbsolutePath.TrimStart('/');

            // Remove bucket name from key if present
            if (s3Key.StartsWith(_settings.BucketName + "/", StringComparison.OrdinalIgnoreCase))
            {
                s3Key = s3Key.Substring(_settings.BucketName.Length + 1);
            }

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = s3Key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);

            _logger.LogInformation("Successfully deleted image from S3. Key: {S3Key}", s3Key);
            return true;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error while deleting image. ImageUrl: {ImageUrl}", imageUrl);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting image. ImageUrl: {ImageUrl}", imageUrl);
            return false;
        }
    }

    public Task<string> GetImageUrlAsync(string s3Key)
    {
        if (string.IsNullOrWhiteSpace(s3Key))
            throw new ArgumentException("S3Key cannot be empty.", nameof(s3Key));

        // Construct and return public S3 URL from S3 key
        var imageUrl = $"https://{_settings.BucketName}.s3.{_settings.Region}.amazonaws.com/{s3Key}";
        return Task.FromResult(imageUrl);
    }
}

