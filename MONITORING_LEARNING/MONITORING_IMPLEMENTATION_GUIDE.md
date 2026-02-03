# Monitoring Implementation Guide - Step by Step

This guide provides **practical, hands-on implementations** of monitoring concepts for your Fee Management Service API.

---

## 🎯 Quick Start: Choose Your Path

### **Option A: Start Simple (Recommended for Beginners)**
1. Enhanced Health Checks
2. Seq Log Aggregation
3. Basic Metrics with Prometheus

### **Option B: Cloud-Native (If using Azure/AWS)**
1. Application Insights (Azure) or CloudWatch (AWS)
2. Health Checks
3. Custom Metrics

### **Option C: Full Observability Stack**
1. OpenTelemetry
2. Prometheus + Grafana
3. Jaeger for Tracing

---

## 📋 Implementation 1: Enhanced Health Checks

### **Step 1: Install Required Packages**

```bash
cd FeeManagementService
dotnet add package AspNetCore.HealthChecks.SqlServer
dotnet add package AspNetCore.HealthChecks.System
dotnet add package AspNetCore.HealthChecks.UI
dotnet add package AspNetCore.HealthChecks.UI.Client
dotnet add package AspNetCore.HealthChecks.UI.InMemory.Storage
```

### **Step 2: Update Program.cs**

```csharp
// Add after builder.Services.AddDbContext<FeeDbContext>

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: connectionString,
        name: "database",
        tags: new[] { "db", "sql", "ready" },
        timeout: TimeSpan.FromSeconds(3))
    .AddCheck<S3HealthCheck>(
        name: "s3",
        tags: new[] { "aws", "storage", "ready" })
    .AddCheck<JwtServiceHealthCheck>(
        name: "jwt-service",
        tags: new[] { "auth", "ready" })
    .AddCheck("memory", () =>
    {
        var allocated = GC.GetTotalMemory(forceFullCollection: false);
        var data = new Dictionary<string, object>
        {
            { "AllocatedBytes", allocated },
            { "Gen0Collections", GC.CollectionCount(0) },
            { "Gen1Collections", GC.CollectionCount(1) },
            { "Gen2Collections", GC.CollectionCount(2) }
        };
        
        var status = allocated < 1024L * 1024L * 1024L * 2 // 2GB
            ? HealthStatus.Healthy
            : HealthStatus.Degraded;
            
        return HealthCheckResult.Healthy("Memory check passed", data);
    }, tags: new[] { "memory", "ready" });

// Health Check UI (optional, for visualization)
builder.Services.AddHealthChecksUI(settings =>
{
    settings.SetEvaluationTimeInSeconds(10);
    settings.AddHealthCheckEndpoint("Fee Management API", "/health");
})
.AddInMemoryStorage();
```

### **Step 3: Create Custom Health Checks**

Create `Services/S3HealthCheck.cs`:

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using FeeManagementService.Services;

namespace FeeManagementService.Services;

public class S3HealthCheck : IHealthCheck
{
    private readonly IS3Service _s3Service;
    private readonly ILogger<S3HealthCheck> _logger;

    public S3HealthCheck(IS3Service s3Service, ILogger<S3HealthCheck> logger)
    {
        _s3Service = s3Service;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to list buckets or perform a simple S3 operation
            // This is a lightweight check
            var isHealthy = await _s3Service.CheckConnectivityAsync(cancellationToken);
            
            if (isHealthy)
            {
                return HealthCheckResult.Healthy("S3 service is accessible");
            }
            
            return HealthCheckResult.Unhealthy("S3 service is not accessible");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3 health check failed");
            return HealthCheckResult.Unhealthy(
                "S3 health check failed",
                exception: ex);
        }
    }
}
```

Create `Services/JwtServiceHealthCheck.cs`:

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using FeeManagementService.Configuration;

namespace FeeManagementService.Services;

public class JwtServiceHealthCheck : IHealthCheck
{
    private readonly JwtSettings _jwtSettings;

    public JwtServiceHealthCheck(IConfiguration configuration)
    {
        _jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_jwtSettings == null || string.IsNullOrEmpty(_jwtSettings.SecretKey))
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("JWT settings are not configured"));
        }

        if (_jwtSettings.SecretKey.Length < 32)
        {
            return Task.FromResult(
                HealthCheckResult.Degraded("JWT secret key is too short (minimum 32 characters)"));
        }

        return Task.FromResult(
            HealthCheckResult.Healthy("JWT service is configured correctly"));
    }
}
```

### **Step 4: Add Health Check Interface to IS3Service**

Update `Services/IS3Service.cs`:

```csharp
Task<bool> CheckConnectivityAsync(CancellationToken cancellationToken = default);
```

Update `Services/S3Service.cs`:

```csharp
public async Task<bool> CheckConnectivityAsync(CancellationToken cancellationToken = default)
{
    try
    {
        // Perform a lightweight S3 operation to check connectivity
        await _s3Client.GetBucketLocationAsync(_settings.BucketName, cancellationToken);
        return true;
    }
    catch
    {
        return false;
    }
}
```

### **Step 5: Map Health Check Endpoints**

In `Program.cs`, add before `app.Run()`:

```csharp
// Health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false, // No checks for liveness
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Health Check UI (optional)
app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
    options.ApiPath = "/health-api";
});
```

### **Step 6: Test Health Checks**

```bash
# Start your API
dotnet run

# Test health endpoints
curl http://localhost:5000/health
curl http://localhost:5000/health/ready
curl http://localhost:5000/health/live

# View Health Check UI
# Navigate to: http://localhost:5000/health-ui
```

---

## 📋 Implementation 2: Seq Log Aggregation

### **Step 1: Install Seq**

**Option A: Docker (Recommended)**
```bash
docker run -d --name seq -p 5341:80 -p 5342:443 -e ACCEPT_EULA=Y datalust/seq:latest
```

**Option B: Download**
- Download from: https://datalust.co/seq
- Install and run locally

### **Step 2: Install Serilog Seq Sink**

```bash
dotnet add package Serilog.Sinks.Seq
```

### **Step 3: Update Program.cs**

Update the Serilog configuration:

```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "FeeManagementService")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/fee-management-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.Seq(
        serverUrl: builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341",
        apiKey: builder.Configuration["Seq:ApiKey"]) // Optional
    .CreateLogger();
```

### **Step 4: Add Seq Configuration**

In `appsettings.json`:

```json
{
  "Seq": {
    "ServerUrl": "http://localhost:5341",
    "ApiKey": "" // Optional, for production
  }
}
```

### **Step 5: Enhance Logging with More Context**

Update `RequestLoggingMiddleware.cs`:

```csharp
if (shouldLog)
{
    _logger.LogInformation(
        "Incoming request: {Method} {Path}, SchoolId: {SchoolId}, UserId: {UserId}, CorrelationId: {CorrelationId}, IP: {IpAddress}, UserAgent: {UserAgent}",
        method, path, schoolId, userId, correlationId,
        context.Connection.RemoteIpAddress?.ToString(),
        context.Request.Headers["User-Agent"].ToString());
}
```

### **Step 6: Test Seq**

1. Start your API
2. Make some API calls
3. Open Seq UI: http://localhost:5341
4. Search for logs using queries like:
   - `SchoolId = 'your-school-id'`
   - `StatusCode >= 400`
   - `ElapsedMs > 1000`

---

## 📋 Implementation 3: Prometheus Metrics

### **Step 1: Install Prometheus.NET**

```bash
dotnet add package prometheus-net.AspNetCore
dotnet add package prometheus-net.SystemMetrics
```

### **Step 2: Update Program.cs**

Add after `builder.Services.AddControllers()`:

```csharp
// Prometheus metrics
builder.Services.AddSingleton<MetricReporter>();
```

Add before `app.Run()`:

```csharp
// Prometheus metrics endpoint
app.UseMetricServer(); // Exposes /metrics endpoint
app.UseHttpMetrics(); // HTTP request metrics

// System metrics (CPU, memory, etc.)
app.UseSystemMetrics();
```

### **Step 3: Create MetricReporter Service**

Create `Services/MetricReporter.cs`:

```csharp
using Prometheus;

namespace FeeManagementService.Services;

public class MetricReporter
{
    // Counters - incrementing values
    private readonly Counter _feeCreatedCounter;
    private readonly Counter _s3UploadCounter;
    private readonly Counter _s3UploadFailureCounter;
    private readonly Counter _apiErrorCounter;
    
    // Histograms - distribution of values (response times)
    private readonly Histogram _feeCreationDuration;
    private readonly Histogram _s3UploadDuration;
    private readonly Histogram _jwtGenerationDuration;
    
    // Gauges - current values
    private readonly Gauge _activeFeesGauge;
    private readonly Gauge _databaseConnectionPoolSize;

    public MetricReporter()
    {
        _feeCreatedCounter = Metrics
            .CreateCounter("fees_created_total", "Total number of fees created", new[] { "school_id", "fee_type" });
        
        _s3UploadCounter = Metrics
            .CreateCounter("s3_uploads_total", "Total number of S3 uploads", new[] { "status" });
        
        _s3UploadFailureCounter = Metrics
            .CreateCounter("s3_upload_failures_total", "Total number of S3 upload failures");
        
        _apiErrorCounter = Metrics
            .CreateCounter("api_errors_total", "Total number of API errors", new[] { "endpoint", "status_code" });
        
        _feeCreationDuration = Metrics
            .CreateHistogram("fee_creation_duration_seconds", "Duration of fee creation", new[] { "school_id" });
        
        _s3UploadDuration = Metrics
            .CreateHistogram("s3_upload_duration_seconds", "Duration of S3 uploads");
        
        _jwtGenerationDuration = Metrics
            .CreateHistogram("jwt_generation_duration_seconds", "Duration of JWT token generation");
        
        _activeFeesGauge = Metrics
            .CreateGauge("active_fees_count", "Current number of active fees", new[] { "school_id" });
    }

    public void RecordFeeCreated(string schoolId, string feeType)
    {
        _feeCreatedCounter.WithLabels(schoolId, feeType).Inc();
    }

    public void RecordS3Upload(string status)
    {
        _s3UploadCounter.WithLabels(status).Inc();
    }

    public void RecordS3UploadFailure()
    {
        _s3UploadFailureCounter.Inc();
    }

    public void RecordApiError(string endpoint, int statusCode)
    {
        _apiErrorCounter.WithLabels(endpoint, statusCode.ToString()).Inc();
    }

    public IDisposable MeasureFeeCreation(string schoolId)
    {
        return _feeCreationDuration.WithLabels(schoolId).NewTimer();
    }

    public IDisposable MeasureS3Upload()
    {
        return _s3UploadDuration.NewTimer();
    }

    public IDisposable MeasureJwtGeneration()
    {
        return _jwtGenerationDuration.NewTimer();
    }

    public void SetActiveFeesCount(string schoolId, int count)
    {
        _activeFeesGauge.WithLabels(schoolId).Set(count);
    }
}
```

### **Step 4: Use Metrics in Your Services**

Update `Services/FeeService.cs`:

```csharp
private readonly MetricReporter _metrics;

public FeeService(..., MetricReporter metrics)
{
    // ...
    _metrics = metrics;
}

public async Task<Fee> CreateFeeAsync(CreateFeeRequest request, string schoolId, string userId)
{
    using (_metrics.MeasureFeeCreation(schoolId))
    {
        // ... existing fee creation logic ...
        
        _metrics.RecordFeeCreated(schoolId, request.FeeType.ToString());
        return fee;
    }
}
```

Update `Services/S3Service.cs`:

```csharp
private readonly MetricReporter _metrics;

public S3Service(..., MetricReporter metrics)
{
    // ...
    _metrics = metrics;
}

public async Task<PresignedUrlResponse> GeneratePresignedUrlAsync(...)
{
    using (_metrics.MeasureS3Upload())
    {
        try
        {
            // ... existing logic ...
            _metrics.RecordS3Upload("success");
            return response;
        }
        catch
        {
            _metrics.RecordS3UploadFailure();
            throw;
        }
    }
}
```

### **Step 5: Update GlobalExceptionHandlerMiddleware**

```csharp
private readonly MetricReporter _metrics;

public GlobalExceptionHandlerMiddleware(..., MetricReporter metrics)
{
    // ...
    _metrics = metrics;
}

// In InvokeAsync method, when handling exceptions:
_metrics.RecordApiError(context.Request.Path, context.Response.StatusCode);
```

### **Step 6: Test Prometheus Metrics**

```bash
# Start your API
dotnet run

# View metrics endpoint
curl http://localhost:5000/metrics

# You should see metrics like:
# fees_created_total{school_id="...", fee_type="..."} 5.0
# fee_creation_duration_seconds_sum{school_id="..."} 0.123
```

---

## 📋 Implementation 4: OpenTelemetry Distributed Tracing

### **Step 1: Install OpenTelemetry Packages**

```bash
dotnet add package OpenTelemetry.Exporter.Console
dotnet add package OpenTelemetry.Exporter.Jaeger
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore
```

### **Step 2: Update Program.cs**

Add OpenTelemetry configuration:

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// Add after builder.Services.AddControllers()

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "FeeManagementService",
            serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.EnrichWithHttpRequest = (activity, request) =>
            {
                activity.SetTag("http.request.school_id", request.Headers["X-School-Id"]);
            };
        })
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
        })
        .AddConsoleExporter()
        .AddJaegerExporter(options =>
        {
            options.AgentHost = builder.Configuration["Jaeger:AgentHost"] ?? "localhost";
            options.AgentPort = int.Parse(builder.Configuration["Jaeger:AgentPort"] ?? "6831");
        }));
```

### **Step 3: Add Jaeger Configuration**

In `appsettings.json`:

```json
{
  "Jaeger": {
    "AgentHost": "localhost",
    "AgentPort": "6831"
  }
}
```

### **Step 4: Run Jaeger (Docker)**

```bash
docker run -d --name jaeger \
  -p 16686:16686 \
  -p 6831:6831/udp \
  jaegertracing/all-in-one:latest
```

### **Step 5: Add Custom Spans**

Update `Services/S3Service.cs`:

```csharp
using System.Diagnostics;

public async Task<PresignedUrlResponse> GeneratePresignedUrlAsync(...)
{
    using var activity = ActivitySource.StartActivity("S3.GeneratePresignedUrl");
    activity?.SetTag("s3.bucket", _settings.BucketName);
    activity?.SetTag("s3.file_name", request.FileName);
    
    try
    {
        // ... existing logic ...
        activity?.SetStatus(ActivityStatusCode.Ok);
        return response;
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.RecordException(ex);
        throw;
    }
}
```

Add ActivitySource:

```csharp
private static readonly ActivitySource ActivitySource = new("FeeManagementService.S3");

public S3Service(...)
{
    // ...
}
```

### **Step 6: Test Tracing**

1. Start your API
2. Make API calls
3. Open Jaeger UI: http://localhost:16686
4. Search for traces and see the complete request flow

---

## 📋 Implementation 5: Error Tracking with Sentry

### **Step 1: Install Sentry**

```bash
dotnet add package Sentry.AspNetCore
```

### **Step 2: Update Program.cs**

```csharp
using Sentry;

// Add before builder.Services.AddControllers()

builder.WebHost.UseSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:Dsn"];
    options.Environment = builder.Environment.EnvironmentName;
    options.TracesSampleRate = 1.0; // 100% of transactions (adjust for production)
    options.ProfilesSampleRate = 1.0; // 100% of profiles
});

// Add Sentry to services
builder.Services.AddSentry();
```

### **Step 3: Add Sentry Configuration**

In `appsettings.json`:

```json
{
  "Sentry": {
    "Dsn": "YOUR_SENTRY_DSN" // Get from https://sentry.io
  }
}
```

### **Step 4: Enhance Exception Logging**

Update `GlobalExceptionHandlerMiddleware.cs`:

```csharp
using Sentry;

// In catch block:
SentrySdk.CaptureException(ex, scope =>
{
    scope.SetTag("endpoint", context.Request.Path);
    scope.SetTag("method", context.Request.Method);
    scope.SetTag("school_id", context.GetSchoolId() ?? "unknown");
    scope.SetUser(new User
    {
        Id = context.GetUserId() ?? "unknown"
    });
    scope.SetExtra("request_body", await ReadRequestBodyAsync(context.Request));
});
```

### **Step 5: Test Sentry**

1. Sign up at https://sentry.io (free tier available)
2. Create a project and get DSN
3. Add DSN to appsettings.json
4. Trigger an error in your API
5. Check Sentry dashboard for the error

---

## 🎯 Next Steps

After implementing these basics:

1. **Set up Grafana** - Create beautiful dashboards
2. **Configure Alerting** - Get notified of issues
3. **Add Business Metrics** - Track fee creation, revenue, etc.
4. **Implement Log Correlation** - Track requests across services
5. **Set up CI/CD Monitoring** - Monitor deployments

---

## 📚 Resources

- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Serilog Documentation](https://serilog.net/)
- [Prometheus.NET](https://github.com/prometheus-net/prometheus-net)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Sentry .NET](https://docs.sentry.io/platforms/dotnet/)

---

**Happy Monitoring! 🚀**

