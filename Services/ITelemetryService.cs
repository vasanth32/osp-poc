namespace FeeManagementService.Services;

/// <summary>
/// Service for tracking custom telemetry to Application Insights
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Track when a fee is created
    /// </summary>
    void TrackFeeCreated(string schoolId, string feeType, decimal amount);

    /// <summary>
    /// Track S3 upload operations
    /// </summary>
    void TrackS3Upload(string schoolId, string fileName, long fileSize, bool success, TimeSpan duration);

    /// <summary>
    /// Track JWT token generation
    /// </summary>
    void TrackJwtTokenGenerated(string userId, string schoolId);

    /// <summary>
    /// Track custom events
    /// </summary>
    void TrackCustomEvent(string eventName, Dictionary<string, string> properties);

    /// <summary>
    /// Track custom metrics
    /// </summary>
    void TrackCustomMetric(string metricName, double value, Dictionary<string, string>? properties = null);

    /// <summary>
    /// Track exceptions with context
    /// </summary>
    void TrackException(Exception exception, Dictionary<string, string>? properties = null);
}

