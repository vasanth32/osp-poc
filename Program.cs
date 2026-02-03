using System.Security.Claims;
using System.Text;
using Amazon;
using Amazon.S3;
using Amazon.Runtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Sinks.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using FeeManagementService.Data;
using FeeManagementService.Configuration;
using FeeManagementService.Services;
using FeeManagementService.Middleware;
using FluentValidation.AspNetCore;
using FluentValidation;
using FeeManagementService.Validators;

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

// Only add Application Insights if connection string is provided
if (!string.IsNullOrWhiteSpace(applicationInsightsOptions.ConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(applicationInsightsOptions);
}

// Configure Serilog
var loggerConfig = new LoggerConfiguration()
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
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

// Add Application Insights sink if connection string is provided
if (!string.IsNullOrWhiteSpace(applicationInsightsOptions.ConnectionString))
{
    loggerConfig.WriteTo.ApplicationInsights(
        serviceProvider: builder.Services.BuildServiceProvider(),
        telemetryConverter: TelemetryConverter.Traces);
}

Log.Logger = loggerConfig.CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Configure Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<FeeDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (jwtSettings != null)
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            // Map role claims correctly for [Authorize(Roles = "...")] to work
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });
}

// Configure AWS S3 Settings
builder.Services.Configure<AwsS3Settings>(builder.Configuration.GetSection("AwsS3"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Configure AWS S3 Client
var awsS3Settings = builder.Configuration.GetSection("AwsS3").Get<AwsS3Settings>();
if (awsS3Settings != null)
{
    IAmazonS3 s3Client;
    
    if (!string.IsNullOrWhiteSpace(awsS3Settings.AccessKey) && 
        !string.IsNullOrWhiteSpace(awsS3Settings.SecretKey))
    {
        // Use BasicAWSCredentials if AccessKey and SecretKey are provided
        var credentials = new BasicAWSCredentials(awsS3Settings.AccessKey, awsS3Settings.SecretKey);
        var region = RegionEndpoint.GetBySystemName(awsS3Settings.Region);
        s3Client = new AmazonS3Client(credentials, region);
    }
    else
    {
        // Use default credential chain (IAM role, environment variables, etc.)
        var region = RegionEndpoint.GetBySystemName(awsS3Settings.Region);
        s3Client = new AmazonS3Client(region);
    }
    
    builder.Services.AddSingleton<IAmazonS3>(s3Client);
    builder.Services.AddScoped<IS3Service, S3Service>();
}

// Register Fee Service
builder.Services.AddScoped<IFeeService, FeeService>();

// Register JWT Token Service
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Register Telemetry Service (only if Application Insights is configured)
if (!string.IsNullOrWhiteSpace(applicationInsightsOptions.ConnectionString))
{
    builder.Services.AddSingleton<ITelemetryService, TelemetryService>();
}
else
{
    // Use a no-op implementation when Application Insights is not configured
    builder.Services.AddSingleton<ITelemetryService, NoOpTelemetryService>();
}

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<CreateFeeRequestValidator>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fee Management Service API",
        Version = "v1",
        Description = "ASP.NET Core 8.0 Web API for Fee Management with AWS S3 image uploads using presigned URLs",
        Contact = new OpenApiContact
        {
            Name = "Fee Management Service",
            Email = "support@feemanagement.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fee Management Service API v1");
    });
}

app.UseHttpsRedirection();

// Request Logging Middleware (should be early in pipeline)
app.UseMiddleware<RequestLoggingMiddleware>();

// Use CORS
app.UseCors("AllowAll");

// Use Authentication & Authorization
app.UseAuthentication();

// Tenant Middleware - Extract SchoolId, UserId, and Role from JWT
app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();

// Global Exception Handler (should be after all middleware, before controllers)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Map Controllers
app.MapControllers();

app.Run();
