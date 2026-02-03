using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace FeeManagementService.Services;

/// <summary>
/// Service for tracking custom telemetry to Application Insights
/// </summary>
public class TelemetryService : ITelemetryService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<TelemetryService> _logger;

    public TelemetryService(TelemetryClient telemetryClient, ILogger<TelemetryService> logger)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public void TrackFeeCreated(string schoolId, string feeType, decimal amount)
    {
        var properties = new Dictionary<string, string>
        {
            { "SchoolId", schoolId },
            { "FeeType", feeType },
            { "Amount", amount.ToString("F2") }
        };

        var metrics = new Dictionary<string, double>
        {
            { "FeeAmount", (double)amount }
        };

        // Track as custom event
        _telemetryClient.TrackEvent("FeeCreated", properties, metrics);
        
        _logger.LogInformation("Tracked fee creation: SchoolId={SchoolId}, FeeType={FeeType}, Amount={Amount}",
            schoolId, feeType, amount);
    }

    public void TrackS3Upload(string schoolId, string fileName, long fileSize, bool success, TimeSpan duration)
    {
        var properties = new Dictionary<string, string>
        {
            { "SchoolId", schoolId },
            { "FileName", fileName },
            { "FileSize", fileSize.ToString() },
            { "Success", success.ToString() }
        };

        var metrics = new Dictionary<string, double>
        {
            { "UploadDurationMs", duration.TotalMilliseconds },
            { "FileSizeBytes", fileSize }
        };

        _telemetryClient.TrackEvent("S3Upload", properties, metrics);
        
        if (!success)
        {
            _telemetryClient.TrackTrace($"S3 upload failed: {fileName}", SeverityLevel.Warning, properties);
        }
    }

    public void TrackJwtTokenGenerated(string userId, string schoolId)
    {
        var properties = new Dictionary<string, string>
        {
            { "UserId", userId },
            { "SchoolId", schoolId }
        };

        _telemetryClient.TrackEvent("JwtTokenGenerated", properties);
    }

    public void TrackCustomEvent(string eventName, Dictionary<string, string> properties)
    {
        _telemetryClient.TrackEvent(eventName, properties);
    }

    public void TrackCustomMetric(string metricName, double value, Dictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackMetric(metricName, value, properties);
    }

    public void TrackException(Exception exception, Dictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackException(exception, properties);
    }
}

