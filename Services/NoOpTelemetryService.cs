namespace FeeManagementService.Services;

/// <summary>
/// No-op implementation of ITelemetryService when Application Insights is not configured
/// </summary>
public class NoOpTelemetryService : ITelemetryService
{
    public void TrackFeeCreated(string schoolId, string feeType, decimal amount)
    {
        // No-op
    }

    public void TrackS3Upload(string schoolId, string fileName, long fileSize, bool success, TimeSpan duration)
    {
        // No-op
    }

    public void TrackJwtTokenGenerated(string userId, string schoolId)
    {
        // No-op
    }

    public void TrackCustomEvent(string eventName, Dictionary<string, string> properties)
    {
        // No-op
    }

    public void TrackCustomMetric(string metricName, double value, Dictionary<string, string>? properties = null)
    {
        // No-op
    }

    public void TrackException(Exception exception, Dictionary<string, string>? properties = null)
    {
        // No-op
    }
}

