# Fee Management Service

ASP.NET Core 8.0 Web API for managing school fees with AWS S3 image upload capabilities using presigned URLs.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Features](#features)
- [Setup Instructions](#setup-instructions)
- [AWS S3 Configuration](#aws-s3-configuration)
- [Database Setup](#database-setup)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [API Documentation](#api-documentation)
- [Testing](#testing)
- [Complete Workflow](#complete-workflow)
- [Troubleshooting](#troubleshooting)

## Prerequisites

- **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** - SQL Server 2019 or later, or SQL Server Express
- **AWS Account** - For S3 bucket access
- **Visual Studio 2022** or **VS Code** (optional, for development)
- **Postman** or **Swagger UI** (for API testing)

## Features

- ✅ JWT Authentication with role-based authorization
- ✅ Multi-tenant support (SchoolId isolation)
- ✅ Fee CRUD operations
- ✅ AWS S3 image uploads using presigned URLs
- ✅ Comprehensive validation (FluentValidation)
- ✅ Global error handling with ProblemDetails
- ✅ Request/response logging with Serilog
- ✅ Swagger/OpenAPI documentation
- ✅ Entity Framework Core with SQL Server

## Setup Instructions

### 1. Clone the Repository

```bash
git clone <repository-url>
cd FeeManagementService
```

### 2. Restore NuGet Packages

```bash
dotnet restore
```

### 3. Configure Database Connection

Update `appsettings.json` with your SQL Server connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=FeeManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

### 4. Run Database Migrations

```bash
dotnet ef database update --context FeeDbContext
```

This will create the `FeeManagementDb` database and `Fees` table with all indexes and constraints.

### 5. Configure AWS S3

See [AWS S3 Configuration](#aws-s3-configuration) section below.

### 6. Configure JWT Settings

Update `appsettings.json` with your JWT settings:

```json
{
  "JwtSettings": {
    "Issuer": "FeeManagementService",
    "Audience": "FeeManagementService",
    "SecretKey": "your-super-secret-key-that-should-be-at-least-32-characters-long-for-jwt-signing",
    "ExpirationMinutes": 60
  }
}
```

**Important:** Use a strong secret key (at least 32 characters) in production.

### 7. Run the Application

```bash
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## AWS S3 Configuration

### 1. Create S3 Bucket

1. Log in to AWS Console
2. Navigate to S3 service
3. Click "Create bucket"
4. Enter bucket name (e.g., `school-platform-fees-vasanth`)
5. Select region (e.g., `us-east-1`)
6. Configure settings:
   - **Block Public Access:** Keep enabled for security
   - **Versioning:** Optional (recommended for production)
   - **Encryption:** Enable server-side encryption (AES256)

### 2. Configure CORS

1. Select your bucket → **Permissions** tab
2. Scroll to **Cross-origin resource sharing (CORS)**
3. Click **Edit** and add the following configuration:

```json
[
    {
        "AllowedHeaders": [
            "*"
        ],
        "AllowedMethods": [
            "PUT",
            "GET",
            "HEAD"
        ],
        "AllowedOrigins": [
            "*"
        ],
        "ExposeHeaders": [
            "ETag"
        ],
        "MaxAgeSeconds": 3000
    }
]
```

**Note:** In production, replace `"*"` in `AllowedOrigins` with your specific domain(s).

### 3. Set Up IAM User/Role

#### Option A: IAM User (for POC/Development)

1. Navigate to IAM → **Users** → **Create user**
2. Create user with programmatic access
3. Attach policy: `AmazonS3FullAccess` (or create custom policy with minimal permissions)
4. Save **Access Key ID** and **Secret Access Key**

#### Option B: IAM Role (Recommended for Production)

1. Navigate to IAM → **Roles** → **Create role**
2. Select **EC2** or **ECS** as trusted entity
3. Attach policy: `AmazonS3FullAccess` (or custom policy)
4. Use instance profile (no access keys needed)

### 4. Configure Bucket Policy (Optional)

For additional security, create a bucket policy:

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "AllowPresignedUrlUploads",
            "Effect": "Allow",
            "Principal": "*",
            "Action": [
                "s3:PutObject",
                "s3:GetObject"
            ],
            "Resource": "arn:aws:s3:::school-platform-fees-vasanth/*",
            "Condition": {
                "StringEquals": {
                    "s3:x-amz-server-side-encryption": "AES256"
                }
            }
        }
    ]
}
```

### 5. Update Application Configuration

Update `appsettings.Development.json` (create if it doesn't exist):

```json
{
  "AwsS3": {
    "BucketName": "school-platform-fees-vasanth",
    "Region": "us-east-1",
    "AccessKey": "YOUR_ACCESS_KEY",
    "SecretKey": "YOUR_SECRET_KEY"
  }
}
```

**Note:** `appsettings.Development.json` is gitignored and won't be committed.

## Database Setup

### Initial Migration

The initial migration has already been created. To apply it:

```bash
dotnet ef database update --context FeeDbContext
```

### Create New Migration

If you need to create a new migration:

```bash
dotnet ef migrations add MigrationName --context FeeDbContext
```

### Rollback Migration

To rollback the last migration:

```bash
dotnet ef database update PreviousMigrationName --context FeeDbContext
```

## Configuration

### Environment Variables

For production, use environment variables instead of appsettings.json:

```bash
# Database
ConnectionStrings__DefaultConnection="Server=..."

# AWS S3
AwsS3__BucketName="your-bucket-name"
AwsS3__Region="us-east-1"
AwsS3__AccessKey="your-access-key"
AwsS3__SecretKey="your-secret-key"

# JWT
JwtSettings__Issuer="FeeManagementService"
JwtSettings__Audience="FeeManagementService"
JwtSettings__SecretKey="your-secret-key"
JwtSettings__ExpirationMinutes=60
```

### Logging

Logs are written to:
- **Console:** Development environment
- **File:** `logs/fee-management-YYYY-MM-DD.log` (30 days retention)

Configure log levels in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning"
      }
    }
  }
}
```

## Running the Application

### Development

```bash
dotnet run
```

### Production

```bash
dotnet publish -c Release -o ./publish
cd publish
dotnet FeeManagementService.dll
```

## API Documentation

### Swagger UI

Once the application is running, access Swagger UI at:
- `https://localhost:5001/swagger`

Swagger UI provides:
- Interactive API documentation
- Try-it-out functionality
- JWT authentication support
- Request/response examples

### API Endpoints

#### 1. Generate Presigned URL

**POST** `/api/fees/presigned-url`

Generates a presigned URL for direct image upload to S3.

**Authorization:** Required (SchoolAdmin role)

**Request Body:**
```json
{
  "feeId": "fee-123",
  "fileName": "class-fee-image.jpg",
  "contentType": "image/jpeg",
  "fileSize": 102400
}
```

**Response:** `200 OK`
```json
{
  "presignedUrl": "https://bucket.s3.region.amazonaws.com/...?X-Amz-Signature=...",
  "s3Key": "schools/{schoolId}/fees/{feeId}/image.jpg",
  "imageUrl": "https://bucket.s3.region.amazonaws.com/schools/{schoolId}/fees/{feeId}/image.jpg",
  "expiresAt": "2026-02-01T15:30:00Z"
}
```

#### 2. Create Fee

**POST** `/api/fees`

Creates a new fee with optional image URL.

**Authorization:** Required (SchoolAdmin role)

**Request Body:**
```json
{
  "title": "Class Fee - Semester 1",
  "description": "Fee for first semester classes",
  "amount": 1500.00,
  "feeType": "ClassFee",
  "imageUrl": "https://bucket.s3.region.amazonaws.com/schools/{schoolId}/fees/{feeId}/image.jpg"
}
```

**Response:** `201 Created`
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "schoolId": "550e8400-e29b-41d4-a716-446655440000",
  "title": "Class Fee - Semester 1",
  "description": "Fee for first semester classes",
  "amount": 1500.00,
  "feeType": "ClassFee",
  "imageUrl": "https://bucket.s3.region.amazonaws.com/...",
  "status": "Active",
  "createdBy": "user-123",
  "createdAt": "2026-02-01T14:30:00Z"
}
```

## Testing

### Using Swagger UI

1. Start the application
2. Navigate to `/swagger`
3. Click **Authorize** button
4. Enter JWT token: `Bearer <your-token>`
5. Test endpoints using "Try it out"

### Using Postman

See [POSTMAN_TESTING.md](./POSTMAN_TESTING.md) for complete Postman collection and test cases.

### Test Cases

#### Success Cases:
- ✅ Get presigned URL with valid file metadata
- ✅ Upload image to S3 using presigned URL (PUT request)
- ✅ Create fee with valid imageUrl
- ✅ Create fee without image

#### Error Cases:
- ❌ Missing required fields → 400 Bad Request
- ❌ Invalid amount (negative or zero) → 400 Bad Request
- ❌ File too large (>5MB) in presigned URL request → 400 Bad Request
- ❌ Invalid content type in presigned URL request → 400 Bad Request
- ❌ Invalid imageUrl format in create fee request → 400 Bad Request
- ❌ Unauthorized (no token) → 401 Unauthorized
- ❌ Forbidden (wrong role) → 403 Forbidden
- ❌ Expired presigned URL → 403 Forbidden (from S3)

## Complete Workflow

### Step 1: Get Presigned URL

```http
POST /api/fees/presigned-url
Authorization: Bearer <jwt-token>
Content-Type: application/json

{
  "feeId": "fee-123",
  "fileName": "class-fee.jpg",
  "contentType": "image/jpeg",
  "fileSize": 102400
}
```

**Response:**
```json
{
  "presignedUrl": "https://bucket.s3.region.amazonaws.com/...?X-Amz-Signature=...",
  "s3Key": "schools/{schoolId}/fees/{feeId}/class-fee.jpg",
  "imageUrl": "https://bucket.s3.region.amazonaws.com/schools/{schoolId}/fees/{feeId}/class-fee.jpg",
  "expiresAt": "2026-02-01T15:30:00Z"
}
```

### Step 2: Upload Image to S3

```http
PUT <presignedUrl>
Content-Type: image/jpeg
Content-Length: 102400

[binary image data]
```

**Response:** `200 OK` (from S3)

### Step 3: Create Fee with Image URL

```http
POST /api/fees
Authorization: Bearer <jwt-token>
Content-Type: application/json

{
  "title": "Class Fee - Semester 1",
  "description": "Fee for first semester",
  "amount": 1500.00,
  "feeType": "ClassFee",
  "imageUrl": "https://bucket.s3.region.amazonaws.com/schools/{schoolId}/fees/{feeId}/class-fee.jpg"
}
```

**Response:** `201 Created` with FeeResponse

## Troubleshooting

### Database Connection Issues

**Error:** `A connection was successfully established with the server, but then an error occurred during the login process`

**Solution:** Add `TrustServerCertificate=True` to connection string:

```json
"DefaultConnection": "Server=...;Database=...;Trusted_Connection=True;TrustServerCertificate=True"
```

### AWS S3 Access Denied

**Error:** `Access Denied` when uploading to S3

**Solutions:**
1. Verify IAM user has S3 permissions
2. Check bucket policy allows PUT operations
3. Verify CORS configuration
4. Ensure AccessKey and SecretKey are correct

### JWT Token Issues

**Error:** `401 Unauthorized`

**Solutions:**
1. Verify token includes required claims (SchoolId, UserId, Role)
2. Check token hasn't expired
3. Verify SecretKey matches between token generation and appsettings.json
4. Ensure token issuer and audience match configuration

See [JWT_TESTING.md](./JWT_TESTING.md) for detailed JWT testing guide.

### Presigned URL Expired

**Error:** `403 Forbidden` when uploading to S3

**Solution:** Presigned URLs expire after 10 minutes (default). Generate a new presigned URL.

### Logs Not Appearing

**Issue:** Logs not written to file

**Solutions:**
1. Check `logs/` directory exists and is writable
2. Verify Serilog configuration in appsettings.json
3. Check file permissions

### Migration Errors

**Error:** Migration fails

**Solutions:**
1. Ensure database server is running
2. Verify connection string is correct
3. Check user has CREATE DATABASE permission
4. Delete existing database if recreating: `dotnet ef database drop --context FeeDbContext`

## Project Structure

```
FeeManagementService/
├── Controllers/          # API controllers
├── Models/              # Data models and DTOs
├── Services/             # Business logic services
├── Data/                 # DbContext and database configuration
├── Middleware/           # Custom middleware
├── Validators/           # FluentValidation validators
├── Configuration/        # Configuration classes
├── Migrations/           # Entity Framework migrations
├── logs/                 # Application logs (gitignored)
└── README.md            # This file
```

## Security Best Practices

- ✅ Never commit secrets to source control
- ✅ Use environment variables in production
- ✅ Use IAM roles instead of access keys when possible
- ✅ Enable HTTPS in production
- ✅ Use strong JWT secret keys (at least 32 characters)
- ✅ Rotate credentials regularly
- ✅ Enable S3 bucket encryption
- ✅ Configure CORS properly (not `*` in production)

## Support

For issues and questions:
- Check [PROGRESS.md](./PROGRESS.md) for implementation details
- Review [JWT_TESTING.md](./JWT_TESTING.md) for JWT token testing
- See [POSTMAN_TESTING.md](./POSTMAN_TESTING.md) for API testing

## License

[Your License Here]


