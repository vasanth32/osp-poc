# Azure Application Insights - Learn by Doing Guide

## 🎯 What You'll Learn

By implementing Application Insights in your Fee Management Service, you'll learn:

1. **Telemetry Collection** - Automatic and custom telemetry
2. **Performance Monitoring** - Track API response times, dependencies
3. **Error Tracking** - Capture exceptions with full context
4. **Custom Metrics** - Track business metrics (fees created, uploads, etc.)
5. **Distributed Tracing** - Follow requests across services
6. **Live Metrics** - Real-time monitoring
7. **Log Analytics** - Query and analyze telemetry data
8. **Alerting** - Get notified of issues automatically
9. **Dashboards** - Visualize your application health
10. **Dependency Tracking** - Monitor SQL Server, S3, external calls

---

## 📋 Prerequisites

- Azure account (free tier available)
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- Your Fee Management Service project

---

## 🚀 Step 1: Create Application Insights Resource

### **Option A: Azure Portal (Recommended for Learning)**

1. **Sign in to Azure Portal**
   - Go to https://portal.azure.com
   - Sign in with your Azure account (create one if needed - free tier available)

2. **Create Application Insights Resource**
   - Click "Create a resource"
   - Search for "Application Insights"
   - Click "Create"
   - Fill in the details:
     - **Name**: `fee-management-service-insights` (must be unique)
     - **Resource Group**: Create new or use existing
     - **Region**: Choose closest to you (e.g., `East US`)
     - **Application Type**: `ASP.NET Core`
   - Click "Review + create" → "Create"
   - Wait for deployment (1-2 minutes)

3. **Get Connection String**
   - Once created, go to your Application Insights resource
   - In the left menu, find "Overview"
   - Click on "Connection String" (or look in "Essentials" section)
   - **Copy the Connection String** - You'll need this!

### **Option B: Azure CLI (For Advanced Users)**

```bash
# Login to Azure
az login

# Create resource group
az group create --name rg-fee-management --location eastus

# Create Application Insights
az monitor app-insights component create \
  --app fee-management-service-insights \
  --location eastus \
  --resource-group rg-fee-management \
  --application-type web

# Get connection string
az monitor app-insights component show \
  --app fee-management-service-insights \
  --resource-group rg-fee-management \
  --query connectionString \
  --output tsv
```

---

## 📦 Step 2: Install NuGet Package

```bash
cd FeeManagementService
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

**What this package does:**
- Automatically collects HTTP requests, dependencies, exceptions
- Tracks performance metrics
- Enables distributed tracing
- Provides telemetry client for custom tracking

---

## 🔧 Step 3: Configure Application Insights

### **3.1 Update appsettings.json**

Add Application Insights configuration:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "YOUR_CONNECTION_STRING_HERE"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Microsoft.ApplicationInsights": "Information"
    }
  }
}
```

**For Development (appsettings.Development.json):**

```json
{
  "ApplicationInsights": {
    "ConnectionString": "YOUR_CONNECTION_STRING_HERE",
    "EnableAdaptiveSampling": true,
    "EnablePerformanceCounterCollectionModule": true,
    "EnableQuickPulseMetricStream": true
  }
}
```

### **3.2 Update Program.cs**

Add Application Insights configuration:

```csharp
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;

var builder = WebApplication.CreateBuilder(args);

// Configure Application Insights
var applicationInsightsOptions = new ApplicationInsightsServiceOptions
{
    ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"],
    EnableAdaptiveSampling = builder.Configuration.GetValue<bool>("ApplicationInsights:EnableAdaptiveSampling", true),
    EnablePerformanceCounterCollectionModule = builder.Configuration.GetValue<bool>("ApplicationInsights:EnablePerformanceCounterCollectionModule", true),
    EnableQuickPulseMetricStream = builder.Configuration.GetValue<bool>("ApplicationInsights:EnableQuickPulseMetricStream", true),
    EnableRequestTrackingTelemetryModule = true,
    EnableDependencyTrackingTelemetryModule = true,
    EnableEventCounterCollectionModule = true,
    EnableAppServicesHeartbeatTelemetryModule = true,
    EnableAzureInstanceMetadataTelemetryModule = true
};

builder.Services.AddApplicationInsightsTelemetry(applicationInsightsOptions);

// Configure Serilog to also send logs to Application Insights
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "FeeManagementService")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/fee-management-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.ApplicationInsights(
        serviceProvider: builder.Services.BuildServiceProvider(),
        telemetryConverter: TelemetryConverter.Traces) // Send logs as traces
    .CreateLogger();

builder.Host.UseSerilog();

// ... rest of your existing code ...
```

**Key Configuration Options Explained:**

- `EnableAdaptiveSampling`: Reduces telemetry volume in high-traffic scenarios
- `EnablePerformanceCounterCollectionModule`: Collects CPU, memory, etc.
- `EnableQuickPulseMetricStream`: Enables Live Metrics Stream
- `EnableRequestTrackingTelemetryModule`: Tracks HTTP requests
- `EnableDependencyTrackingTelemetryModule`: Tracks external calls (SQL, HTTP, S3)

### **3.3 Install Serilog Application Insights Sink (Optional but Recommended)**

This allows your Serilog logs to appear in Application Insights:

```bash
dotnet add package Serilog.Sinks.ApplicationInsights
```

---

## 📊 Step 4: Add Custom Telemetry

### **4.1 Create Telemetry Service**

Create `Services/ITelemetryService.cs`:

```csharp
namespace FeeManagementService.Services;

public interface ITelemetryService
{
    void TrackFeeCreated(string schoolId, string feeType, decimal amount);
    void TrackS3Upload(string schoolId, string fileName, long fileSize, bool success, TimeSpan duration);
    void TrackJwtTokenGenerated(string userId, string schoolId);
    void TrackCustomEvent(string eventName, Dictionary<string, string> properties);
    void TrackCustomMetric(string metricName, double value, Dictionary<string, string> properties = null);
    void TrackException(Exception exception, Dictionary<string, string> properties = null);
}
```

Create `Services/TelemetryService.cs`:

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace FeeManagementService.Services;

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

    public void TrackCustomMetric(string metricName, double value, Dictionary<string, string> properties = null)
    {
        _telemetryClient.TrackMetric(metricName, value, properties);
    }

    public void TrackException(Exception exception, Dictionary<string, string> properties = null)
    {
        _telemetryClient.TrackException(exception, properties);
    }
}
```

### **4.2 Register Telemetry Service**

In `Program.cs`, add:

```csharp
// Register Telemetry Service
builder.Services.AddSingleton<ITelemetryService, TelemetryService>();
```

### **4.3 Use Telemetry in Your Services**

**Update `Services/FeeService.cs`:**

```csharp
private readonly ITelemetryService _telemetryService;

public FeeService(
    FeeDbContext context,
    ILogger<FeeService> logger,
    ITelemetryService telemetryService)
{
    _context = context;
    _logger = logger;
    _telemetryService = telemetryService;
}

public async Task<Fee> CreateFeeAsync(CreateFeeRequest request, string schoolId, string userId)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    try
    {
        // ... existing validation logic ...
        
        // Create fee
        var fee = new Fee
        {
            // ... existing code ...
        };

        await _context.Fees.AddAsync(fee);
        await _context.SaveChangesAsync();

        stopwatch.Stop();
        
        // Track custom telemetry
        _telemetryService.TrackFeeCreated(schoolId, request.FeeType.ToString(), request.Amount);
        
        // Track custom metric
        _telemetryService.TrackCustomMetric("FeeCreationDurationMs", stopwatch.ElapsedMilliseconds, 
            new Dictionary<string, string> { { "SchoolId", schoolId } });

        _logger.LogInformation("Fee created successfully: {FeeId}", fee.Id);
        return fee;
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        
        // Track exception with context
        _telemetryService.TrackException(ex, new Dictionary<string, string>
        {
            { "SchoolId", schoolId },
            { "UserId", userId },
            { "Operation", "CreateFee" }
        });
        
        throw;
    }
}
```

**Update `Services/S3Service.cs`:**

```csharp
private readonly ITelemetryService _telemetryService;

public S3Service(
    IAmazonS3 s3Client,
    IOptions<AwsS3Settings> settings,
    ILogger<S3Service> logger,
    ITelemetryService telemetryService)
{
    _s3Client = s3Client;
    _settings = settings.Value;
    _logger = logger;
    _telemetryService = telemetryService;
}

public async Task<PresignedUrlResponse> GeneratePresignedUrlAsync(GeneratePresignedUrlRequest request, string schoolId)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    bool success = false;
    
    try
    {
        // ... existing S3 logic ...
        
        success = true;
        stopwatch.Stop();
        
        // Track S3 upload telemetry
        _telemetryService.TrackS3Upload(
            schoolId,
            request.FileName,
            request.FileSize,
            success,
            stopwatch.Elapsed);

        return response;
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        success = false;
        
        // Track failed upload
        _telemetryService.TrackS3Upload(
            schoolId,
            request.FileName,
            request.FileSize,
            success,
            stopwatch.Elapsed);
        
        _telemetryService.TrackException(ex, new Dictionary<string, string>
        {
            { "SchoolId", schoolId },
            { "FileName", request.FileName },
            { "Operation", "GeneratePresignedUrl" }
        });
        
        throw;
    }
}
```

**Update `Services/JwtTokenService.cs`:**

```csharp
private readonly ITelemetryService _telemetryService;

public JwtTokenService(
    IOptions<JwtSettings> jwtSettings,
    ILogger<JwtTokenService> logger,
    ITelemetryService telemetryService)
{
    _jwtSettings = jwtSettings.Value;
    _logger = logger;
    _telemetryService = telemetryService;
}

public string GenerateToken(string userId, string schoolId, string role)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    try
    {
        // ... existing JWT generation logic ...
        
        stopwatch.Stop();
        
        // Track JWT generation
        _telemetryService.TrackJwtTokenGenerated(userId, schoolId);
        _telemetryService.TrackCustomMetric("JwtGenerationDurationMs", stopwatch.ElapsedMilliseconds);
        
        return token;
    }
    catch (Exception ex)
    {
        _telemetryService.TrackException(ex, new Dictionary<string, string>
        {
            { "Operation", "GenerateToken" },
            { "UserId", userId }
        });
        throw;
    }
}
```

---

## 🔍 Step 5: Enhance Exception Tracking

### **5.1 Update GlobalExceptionHandlerMiddleware**

Update `Middleware/GlobalExceptionHandlerMiddleware.cs`:

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly TelemetryClient _telemetryClient;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        TelemetryClient telemetryClient)
    {
        _next = next;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var schoolId = context.GetSchoolId() ?? "Unknown";
        var userId = context.GetUserId() ?? "Unknown";
        var correlationId = context.TraceIdentifier;

        // Prepare exception properties
        var properties = new Dictionary<string, string>
        {
            { "SchoolId", schoolId },
            { "UserId", userId },
            { "CorrelationId", correlationId },
            { "Path", context.Request.Path },
            { "Method", context.Request.Method },
            { "QueryString", context.Request.QueryString.ToString() }
        };

        // Track exception in Application Insights
        _telemetryClient.TrackException(exception, properties);

        // Set severity level based on exception type
        var severity = exception is ArgumentException 
            ? SeverityLevel.Warning 
            : SeverityLevel.Error;

        _telemetryClient.TrackTrace(
            $"Exception: {exception.Message}",
            severity,
            properties);

        // ... rest of existing exception handling code ...
    }
}
```

---

## 📈 Step 6: Add Dependency Tracking for S3

Application Insights automatically tracks SQL Server dependencies, but for AWS S3, we need custom tracking.

### **6.1 Create S3 Dependency Tracking**

Update `Services/S3Service.cs` to add dependency tracking:

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

public async Task<PresignedUrlResponse> GeneratePresignedUrlAsync(...)
{
    var dependencyTelemetry = new DependencyTelemetry
    {
        Type = "AWS S3",
        Name = $"S3:GeneratePresignedUrl",
        Data = $"Bucket: {_settings.BucketName}, File: {request.FileName}",
        Target = $"s3.amazonaws.com/{_settings.BucketName}",
        Success = false
    };

    var startTime = DateTimeOffset.UtcNow;
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        // ... existing S3 logic ...
        
        dependencyTelemetry.Success = true;
        dependencyTelemetry.Properties.Add("SchoolId", schoolId);
        dependencyTelemetry.Properties.Add("FileName", request.FileName);
        dependencyTelemetry.Properties.Add("FileSize", request.FileSize.ToString());
        
        return response;
    }
    catch (Exception ex)
    {
        dependencyTelemetry.Success = false;
        dependencyTelemetry.Properties.Add("Error", ex.Message);
        _telemetryClient.TrackDependency(dependencyTelemetry);
        throw;
    }
    finally
    {
        stopwatch.Stop();
        dependencyTelemetry.Duration = stopwatch.Elapsed;
        _telemetryClient.TrackDependency(dependencyTelemetry);
    }
}
```

---

## 🧪 Step 7: Test Your Implementation

### **7.1 Run Your Application**

```bash
dotnet run
```

### **7.2 Generate Some Telemetry**

1. **Make API calls:**
   ```bash
   # Login
   POST http://localhost:5000/api/auth/login
   
   # Generate presigned URL
   POST http://localhost:5000/api/fees/presigned-url
   
   # Create fee
   POST http://localhost:5000/api/fees
   ```

2. **Wait 1-2 minutes** for telemetry to appear in Azure Portal

3. **View in Application Insights:**
   - Go to Azure Portal
   - Open your Application Insights resource
   - Click "Live Metrics" to see real-time data
   - Click "Logs" to query telemetry

---

## 📊 Step 8: Explore Application Insights Features

### **8.1 Live Metrics Stream**

1. In Azure Portal, go to your Application Insights resource
2. Click "Live Metrics Stream" in the left menu
3. Make some API calls
4. **Watch real-time metrics:**
   - Incoming requests
   - Outgoing dependencies
   - Server health (CPU, memory)
   - Request rate
   - Failed requests

**What you'll learn:**
- Real-time monitoring capabilities
- Server performance metrics
- Request/response patterns

### **8.2 Application Map**

1. Click "Application Map" in the left menu
2. **See your application architecture:**
   - Your API
   - SQL Server dependency
   - S3 dependency (if tracked)
   - Request flow

**What you'll learn:**
- Visualize dependencies
- Identify bottlenecks
- Understand system architecture

### **8.3 Performance**

1. Click "Performance" in the left menu
2. **Analyze:**
   - Slowest operations
   - Operation duration trends
   - Dependencies performance
   - Database query performance

**What you'll learn:**
- Identify performance bottlenecks
- Track response time trends
- Optimize slow endpoints

### **8.4 Failures**

1. Click "Failures" in the left menu
2. **View:**
   - Exception types
   - Failed requests
   - Exception trends
   - Stack traces

**What you'll learn:**
- Error patterns
- Exception frequency
- Debug production issues

### **8.5 Logs (Log Analytics)**

1. Click "Logs" in the left menu
2. **Try these queries:**

```kusto
// View all requests
requests
| where timestamp > ago(1h)
| project timestamp, name, url, resultCode, duration
| order by timestamp desc

// View custom events (fees created)
customEvents
| where name == "FeeCreated"
| project timestamp, customDimensions.SchoolId, customDimensions.FeeType, customMeasurements.FeeAmount
| order by timestamp desc

// View exceptions
exceptions
| where timestamp > ago(1h)
| project timestamp, type, message, outerMessage, customDimensions
| order by timestamp desc

// View dependencies (SQL, S3)
dependencies
| where timestamp > ago(1h)
| project timestamp, type, name, target, duration, success
| order by timestamp desc

// View S3 uploads
customEvents
| where name == "S3Upload"
| project timestamp, 
    SchoolId = customDimensions.SchoolId,
    FileName = customDimensions.FileName,
    Success = customDimensions.Success,
    Duration = customMeasurements.UploadDurationMs
| order by timestamp desc

// Performance by endpoint
requests
| where timestamp > ago(1h)
| summarize 
    AvgDuration = avg(duration),
    P95Duration = percentile(duration, 95),
    P99Duration = percentile(duration, 99),
    RequestCount = count()
    by name
| order by AvgDuration desc

// Error rate
requests
| where timestamp > ago(1h)
| summarize 
    Total = count(),
    Errors = countif(success == false)
    by bin(timestamp, 5m)
| extend ErrorRate = (Errors * 100.0) / Total
| render timechart

// Top exceptions
exceptions
| where timestamp > ago(24h)
| summarize Count = count() by type, outerMessage
| order by Count desc
| take 10
```

**What you'll learn:**
- Kusto Query Language (KQL)
- Querying telemetry data
- Creating custom reports
- Analyzing trends

### **8.6 Metrics**

1. Click "Metrics" in the left menu
2. **Explore:**
   - Server response time
   - Server requests
   - Failed requests
   - Custom metrics (your TrackMetric calls)

**What you'll learn:**
- Metric visualization
- Time-series analysis
- Custom metric tracking

---

## 🎯 Step 9: Create Custom Dashboards

### **9.1 Create Dashboard**

1. In Azure Portal, click "Dashboards" → "New dashboard"
2. Add tiles for:
   - **Server response time** (Metrics)
   - **Request rate** (Metrics)
   - **Failed requests** (Metrics)
   - **Custom query** (Logs) - Fees created today
   - **Custom query** (Logs) - S3 upload success rate

### **9.2 Pin Custom Queries**

1. In Logs, run a query
2. Click "Pin to dashboard"
3. Choose your dashboard
4. **Example queries to pin:**

```kusto
// Fees created today
customEvents
| where name == "FeeCreated" and timestamp > ago(24h)
| summarize Count = count(), TotalAmount = sum(customMeasurements.FeeAmount) by bin(timestamp, 1h)
| render timechart

// S3 upload success rate
customEvents
| where name == "S3Upload" and timestamp > ago(24h)
| summarize 
    Total = count(),
    Successful = countif(customDimensions.Success == "True")
    by bin(timestamp, 1h)
| extend SuccessRate = (Successful * 100.0) / Total
| project timestamp, SuccessRate
| render timechart
```

**What you'll learn:**
- Dashboard creation
- Visualizing key metrics
- Monitoring at a glance

---

## 🚨 Step 10: Set Up Alerts

### **10.1 Create Alert Rule**

1. In Application Insights, click "Alerts" → "Create" → "Alert rule"
2. **Configure:**
   - **Signal type**: Metric
   - **Signal**: Server response time
   - **Condition**: Average > 1000ms for 5 minutes
   - **Action group**: Create new (email yourself)
   - **Alert rule name**: "High Response Time"

3. **Create more alerts:**
   - **High Error Rate**: Failed requests > 10 in 5 minutes
   - **S3 Upload Failures**: S3Upload events with Success=false > 5 in 10 minutes
   - **Exception Spike**: Exception count > 20 in 5 minutes

**What you'll learn:**
- Proactive monitoring
- Alert configuration
- Incident response

---

## 📚 Learning Exercises

### **Exercise 1: Track Business Metrics**

**Goal**: Track daily fee creation statistics

**Steps**:
1. Create a custom metric for "TotalFeesCreatedToday"
2. Increment it each time a fee is created
3. Query it in Log Analytics
4. Create a dashboard tile

**Code Hint**:
```csharp
_telemetryService.TrackCustomMetric("TotalFeesCreatedToday", 1, 
    new Dictionary<string, string> { { "SchoolId", schoolId } });
```

### **Exercise 2: Track User Activity**

**Goal**: Track which users are most active

**Steps**:
1. Track custom event "UserAction" with UserId
2. Query to find top 10 active users
3. Create a report

### **Exercise 3: Performance Analysis**

**Goal**: Identify slowest endpoints

**Steps**:
1. Query requests table
2. Group by endpoint name
3. Calculate average, p95, p99 response times
4. Identify which endpoints need optimization

### **Exercise 4: Error Analysis**

**Goal**: Understand error patterns

**Steps**:
1. Query exceptions table
2. Group by exception type
3. Find most common errors
4. Create alerts for critical errors

### **Exercise 5: Dependency Health**

**Goal**: Monitor S3 and SQL Server health

**Steps**:
1. Query dependencies table
2. Filter by type (SQL, AWS S3)
3. Calculate success rate
4. Track response times
5. Create alerts for failures

---

## 🎓 Key Concepts You've Learned

✅ **Telemetry Types**:
- Requests (HTTP calls)
- Dependencies (SQL, S3, external APIs)
- Exceptions (errors)
- Events (custom business events)
- Metrics (custom numeric values)
- Traces (log messages)

✅ **Automatic Collection**:
- HTTP requests/responses
- SQL queries
- Dependencies
- Exceptions
- Performance counters

✅ **Custom Tracking**:
- Business events
- Custom metrics
- User actions
- Business KPIs

✅ **Querying**:
- Kusto Query Language (KQL)
- Time-series analysis
- Aggregations
- Filtering

✅ **Visualization**:
- Dashboards
- Charts
- Metrics explorer
- Application map

✅ **Alerting**:
- Metric-based alerts
- Log-based alerts
- Action groups
- Notification channels

---

## 🔗 Additional Resources

- [Application Insights Documentation](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [Kusto Query Language (KQL) Reference](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/query/)
- [Application Insights .NET SDK](https://github.com/microsoft/ApplicationInsights-dotnet)
- [Best Practices](https://learn.microsoft.com/en-us/azure/azure-monitor/app/api-custom-events-metrics)

---

## ✅ Next Steps

1. **Explore more features**:
   - User flows
   - Smart detection
   - Availability tests
   - Profiler

2. **Optimize**:
   - Reduce telemetry volume (sampling)
   - Cost optimization
   - Performance tuning

3. **Integrate**:
   - Power BI
   - Azure Functions
   - Logic Apps

---

**Congratulations! 🎉 You've implemented Application Insights and learned key monitoring concepts!**

