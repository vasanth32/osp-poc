using System.Text;
using Amazon;
using Amazon.S3;
using Amazon.Runtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using FeeManagementService.Data;
using FeeManagementService.Configuration;
using FeeManagementService.Services;
using FluentValidation.AspNetCore;
using FluentValidation;
using FeeManagementService.Validators;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
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
        Description = "ASP.NET Core 8.0 Web API for Fee Management"
    });

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

// Use CORS
app.UseCors("AllowAll");

// Use Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

app.Run();
