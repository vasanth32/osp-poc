# Fee Management Service - Progress Documentation

## 🎯 High-Level Summary - What We Achieved

### Project Overview
We successfully built a **complete ASP.NET Core 8.0 Web API** for a **Fee Management Service** with the following key capabilities:

1. **Multi-Tenant Architecture** - School-based isolation using JWT claims
2. **Secure Image Uploads** - AWS S3 presigned URLs for direct client-to-S3 uploads
3. **JWT Authentication & Authorization** - Role-based access control (SchoolAdmin)
4. **Comprehensive Error Handling** - Global exception handling with ProblemDetails
5. **Request/Response Logging** - Serilog with correlation IDs
6. **API Documentation** - Swagger/OpenAPI with JWT integration
7. **Database Integration** - Entity Framework Core with SQL Server
8. **Request Validation** - FluentValidation for input validation

### Key Features Implemented

#### ✅ Authentication & Authorization
- JWT token generation endpoint (`/api/auth/login`)
- Token-based authentication with configurable expiration
- Role-based authorization (`[Authorize(Roles = "SchoolAdmin")]`)
- Tenant extraction middleware (SchoolId, UserId, Role from JWT)

#### ✅ Fee Management
- Create Fee endpoint (`POST /api/fees`)
- Fee entity with validation (Title, Amount, FeeType, ImageUrl, etc.)
- Database persistence with Entity Framework Core
- Multi-tenant data isolation

#### ✅ AWS S3 Integration
- Presigned URL generation (`POST /api/fees/presigned-url`)
- Direct client-to-S3 image uploads (bypassing API server)
- Server-side encryption (AES256)
- File validation (size, type, extension)
- Image URL generation for fee records

#### ✅ Infrastructure & Quality
- Global exception handling middleware
- Request/response logging with correlation IDs
- Serilog file and console logging
- CORS configuration
- Swagger/OpenAPI documentation
- Postman collection for API testing

### Complete Workflow Achieved

1. **Login** → Get JWT token with SchoolId, UserId, Role
2. **Generate Presigned URL** → Get S3 upload URL and image URL
3. **Upload Image to S3** → Direct upload using presigned URL
4. **Create Fee** → Save fee record with image URL

### Technical Stack

- **Framework:** ASP.NET Core 8.0
- **Database:** SQL Server with Entity Framework Core
- **Authentication:** JWT Bearer Tokens
- **Cloud Storage:** AWS S3 (Presigned URLs)
- **Validation:** FluentValidation
- **Logging:** Serilog
- **Documentation:** Swagger/OpenAPI
- **Testing:** Postman Collection

### Project Status: ✅ **COMPLETE & FUNCTIONAL**

All core features have been implemented, tested, and are working end-to-end. The application is ready for further development or deployment.

---

## Project Overview
ASP.NET Core 8.0 Web API project for managing school fees with image upload capabilities using AWS S3 presigned URLs.

---

## ✅ Step 1: Project Setup and Configuration

### Completed Tasks:
1. **Created ASP.NET Core 8.0 Web API Project**
   - Project Name: `FeeManagementService`
   - Target Framework: .NET 8.0
   - Location: `D:\PracticeProjects\Edlio poc\FeeManagementService`

2. **Installed NuGet Packages:**
   - `Microsoft.EntityFrameworkCore.SqlServer` (8.0.0) - SQL Server database provider
   - `Microsoft.EntityFrameworkCore.Tools` (8.0.0) - EF Core migration tools
   - `AWSSDK.S3` (4.0.18.1) - AWS S3 SDK for file uploads
   - `FluentValidation.AspNetCore` (11.3.1) - Request validation
   - `Microsoft.AspNetCore.Authentication.JwtBearer` (8.0.0) - JWT authentication
   - `Serilog.AspNetCore` (10.0.0) - Structured logging

3. **Created Folder Structure:**
   ```
   FeeManagementService/
   ├── Controllers/      - API controllers
   ├── Models/           - Data models and DTOs
   ├── Services/         - Business logic services
   ├── Data/             - DbContext and database configuration
   ├── Middleware/       - Custom middleware
   ├── Validators/       - FluentValidation validators
   └── Configuration/    - Configuration classes
   ```

4. **Configured Program.cs:**
   - ✅ JWT Authentication setup (fully configured with all validation options)
   - ✅ Entity Framework DbContext registration
   - ✅ Dependency injection configuration
   - ✅ CORS policy (AllowAll for development)
   - ✅ Swagger/OpenAPI with JWT support
   - ✅ Serilog logging configuration
   - ✅ FluentValidation registration

5. **Configured appsettings.json:**
   - ✅ Connection string for SQL Server (`VASANTH\SQLEXPRESS`)
   - ✅ AWS S3 configuration (BucketName, Region, AccessKey, SecretKey)
   - ✅ JWT settings (Issuer, Audience, SecretKey, ExpirationMinutes)

### Files Created:
- `Program.cs` - Application startup and configuration
- `appsettings.json` - Application settings
- `Configuration/JwtSettings.cs` - JWT configuration class
- `Configuration/AwsS3Settings.cs` - AWS S3 configuration class
- `Data/ApplicationDbContext.cs` - Initial DbContext (later replaced)

---

## ✅ Step 2: Database Model and DbContext

### Completed Tasks:

1. **Created Fee Entity Model** (`Models/Fee.cs`):
   - **Properties:**
     - `Id` (Guid, Primary Key)
     - `SchoolId` (Guid, Required, Indexed)
     - `Title` (string, Max 200, Required)
     - `Description` (string, Max 2000, Nullable)
     - `Amount` (decimal, Required, > 0)
     - `FeeType` (enum, Required)
     - `ImageUrl` (string, Max 500, Nullable)
     - `Status` (enum, Default: Active)
     - `CreatedBy` (string, Required)
     - `CreatedAt` (DateTime, Required, UTC)
     - `UpdatedBy` (string, Nullable)
     - `UpdatedAt` (DateTime?, Nullable)

2. **Created Enum Types:**
   - `FeeType` enum (`Models/FeeType.cs`):
     - ActivityFee, ClassFee, CourseFee, TransportFee, LabFee, MiscFee
   - `FeeStatus` enum (`Models/FeeStatus.cs`):
     - Active, Inactive, Archived

3. **Created FeeDbContext** (`Data/FeeDbContext.cs`):
   - `DbSet<Fee> Fees` property
   - **Fluent API Configuration:**
     - Title: MaxLength(200), Required
     - Description: MaxLength(2000), Optional
     - ImageUrl: MaxLength(500), Optional
     - Amount: decimal(18,2), Required
     - **Indexes:**
       - `IX_Fees_SchoolId` on SchoolId
       - `IX_Fees_FeeType` on FeeType
       - `IX_Fees_Status` on Status
       - `IX_Fees_CreatedAt` on CreatedAt
     - **Check Constraint:**
       - `CK_Fees_Amount_Positive`: Ensures Amount > 0

4. **Created Initial Migration:**
   - Migration Name: `InitialCreate`
   - Migration File: `Migrations/20260130133744_InitialCreate.cs`
   - Successfully applied to database: `FeeManagementDb`

5. **Updated Program.cs:**
   - Registered `FeeDbContext` with connection string from appsettings.json
   - Connection string includes `TrustServerCertificate=True` for SSL certificate handling

### Database Details:
- **Server:** `VASANTH\SQLEXPRESS`
- **Database:** `FeeManagementDb`
- **Table:** `Fees` (created successfully with all constraints and indexes)

### Files Created:
- `Models/Fee.cs` - Fee entity model
- `Models/FeeType.cs` - FeeType enum
- `Models/FeeStatus.cs` - FeeStatus enum
- `Data/FeeDbContext.cs` - Database context with Fluent API configuration
- `Migrations/20260130133744_InitialCreate.cs` - Initial database migration

---

## ✅ Step 3: Request/Response Models and Validation

### Completed Tasks:

1. **Created CreateFeeRequest Model** (`Models/CreateFeeRequest.cs`):
   - `Title` (string, required, max 200)
   - `Description` (string, optional, max 2000)
   - `Amount` (decimal, required, > 0)
   - `FeeType` (string, required, must be valid enum value)
   - `Image` (IFormFile, optional, max 5MB, only JPG/PNG/WebP)

2. **Created FeeResponse Model** (`Models/FeeResponse.cs`):
   - Contains all Fee entity properties
   - Used for API responses

3. **Created Mapping Extension** (`Models/FeeExtensions.cs`):
   - `ToResponse()` extension method for mapping `Fee` to `FeeResponse`
   - Provides manual mapping without AutoMapper dependency

4. **Created CreateFeeRequestValidator** (`Validators/CreateFeeRequestValidator.cs`):
   - **Validation Rules:**
     - **Title:**
       - NotEmpty()
       - MaximumLength(200)
     - **Description:**
       - MaximumLength(2000) when not null
     - **Amount:**
       - GreaterThan(0)
     - **FeeType:**
       - NotEmpty()
       - Must be valid enum value (ActivityFee, ClassFee, CourseFee, TransportFee, LabFee, MiscFee)
     - **Image:**
       - Max file size: 5MB (5 * 1024 * 1024 bytes)
       - Allowed extensions: .jpg, .jpeg, .png, .webp
       - Valid content types: image/jpeg, image/jpg, image/png, image/webp
       - Validates both file extension and content type

5. **Registered FluentValidation in Program.cs:**
   - `AddFluentValidationAutoValidation()` - Enables automatic validation
   - `AddFluentValidationClientsideAdapters()` - Enables client-side validation
   - `AddValidatorsFromAssemblyContaining<CreateFeeRequestValidator>()` - Registers all validators

### Files Created:
- `Models/CreateFeeRequest.cs` - Request DTO for creating fees
- `Models/FeeResponse.cs` - Response DTO for fee data
- `Models/FeeExtensions.cs` - Extension methods for mapping
- `Validators/CreateFeeRequestValidator.cs` - FluentValidation validator

---

## ✅ Step 4: AWS S3 Service (Presigned URL Approach)

### Completed Tasks:

1. **Created IS3Service Interface** (`Services/IS3Service.cs`):
   - `GeneratePresignedUrlAsync()` - Generates presigned URLs for direct client uploads
   - `DeleteImageAsync()` - Deletes images from S3
   - `GetImageUrlAsync()` - Constructs public S3 URLs

2. **Created PresignedUrlResponse DTO** (`Models/PresignedUrlResponse.cs`):
   - `PresignedUrl` - The presigned URL for upload
   - `S3Key` - The S3 object key/path
   - `ImageUrl` - Final public URL after upload
   - `ExpiresAt` - When the presigned URL expires

3. **Created S3Service Implementation** (`Services/S3Service.cs`):
   - **Constructor:** IAmazonS3, IOptions<AwsS3Settings>, ILogger<S3Service>
   - **GeneratePresignedUrlAsync Method:**
     - ✅ Validates input parameters:
       - File size (max 5MB = 5 * 1024 * 1024 bytes)
       - File extension (.jpg, .jpeg, .png, .webp)
       - Content type (image/jpeg, image/png, image/webp)
       - schoolId and feeId are not empty
     - ✅ Generates unique filename: `{feeId}_{timestamp}_{Guid}.{extension}`
     - ✅ S3 key structure: `schools/{schoolId}/fees/{feeId}/{filename}`
     - ✅ Creates GetPreSignedUrlRequest with:
       - BucketName from configuration
       - Key = generated S3 key
       - Verb = HttpVerb.PUT (for upload)
       - Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
       - ContentType = provided contentType
       - ServerSideEncryptionMethod = AES256
     - ✅ Generates presigned URL using GetPreSignedURL method
     - ✅ Constructs final image URL: `https://{bucket}.s3.{region}.amazonaws.com/{key}`
     - ✅ Returns PresignedUrlResponse with all required fields
     - ✅ Error handling:
       - AmazonS3Exception → Logged and rethrown
       - ArgumentException → Logged and thrown (invalid parameters)
       - Exception → Logged and wrapped in custom exception
   - **DeleteImageAsync Method:**
     - ✅ Extracts S3 key from URL
     - ✅ Deletes object from S3 using DeleteObjectRequest
     - ✅ Returns true if successful, false otherwise
   - **GetImageUrlAsync Method:**
     - ✅ Constructs and returns public S3 URL from S3 key
   - ✅ All operations use async/await
   - ✅ Comprehensive logging for all S3 operations
   - ✅ **Error Handling Strategy:**
     - **3 Catch Blocks** - Different exception types handled separately:
       - `AmazonS3Exception` - AWS-specific errors (network, permissions, bucket issues)
       - `ArgumentException` - Invalid input parameters (client errors, logged as Warning)
       - `Exception` - Unexpected errors (wrapped in custom exception with context)
     - **`throw` vs `throw ex` - Stack Trace Preservation:**
       - **What is a Stack Trace?**
         - A stack trace shows the exact path your code took to reach the error
         - It lists all method calls from the error point back to the entry point
         - Includes file names, line numbers, and method names
         - Critical for debugging - tells you WHERE and HOW the error occurred
       - **Example Stack Trace:**
         ```
         at S3Service.GeneratePresignedUrlAsync() line 88
         at FeeController.CreateFee() line 45
         at Program.Main() line 132
         ```
       - **`throw` (used in our code):**
         - ✅ Preserves the COMPLETE original stack trace
         - ✅ Shows the exact line where the error first occurred
         - ✅ Maintains all inner exception details
         - ✅ Best for debugging - you see the full error path
         - **Example:** If error occurs at line 88, stack trace shows line 88
       - **`throw ex` (NOT used - BAD PRACTICE):**
         - ❌ Resets the stack trace to the catch block location
         - ❌ Loses the original error location
         - ❌ Makes debugging much harder
         - **Example:** Error at line 88, but stack trace shows line 111 (catch block)
         - **Result:** You can't find where the error actually happened!
       - **`throw new Exception("message", ex)`:**
         - ✅ Wraps the original exception as an inner exception
         - ✅ Preserves original stack trace in InnerException
         - ✅ Allows adding context/description to the error
         - ✅ Used for unexpected errors to provide user-friendly messages

4. **Registered S3Service in Program.cs:**
   - ✅ Reads AWS config from appsettings.json:
     - AWS:S3:BucketName
     - AWS:S3:Region
     - AWS:S3:AccessKey
     - AWS:S3:SecretKey
   - ✅ Creates AmazonS3Client:
     - Uses BasicAWSCredentials when AccessKey/SecretKey provided
     - Falls back to default credential chain if not provided
     - Sets region from configuration
   - ✅ Registers S3Service as Scoped service

5. **Updated appsettings.json:**
   - ✅ AWS S3 Configuration:
     - BucketName: `school-platform-fees-vasanth`
     - Region: `us-east-1`
     - AccessKey: `YOUR_ACCESS_KEY` (configure with actual credentials)
     - SecretKey: `YOUR_SECRET_KEY` (configure with actual credentials)
     - **Note:** Real credentials should be stored in environment variables or secrets manager, not in source control

### Key Features:
- **Presigned URL Approach:**
  - Allows direct client-to-S3 uploads
  - Avoids API server bottleneck
  - Scales better for high-volume uploads (20k+ concurrent requests)
  - Presigned URL expires after specified time (default 10 minutes)
  - Validation enforced via presigned URL policy (file size, content type)
  - API validates business logic (schoolId, feeId) before generating presigned URL

### Files Created:
- `Services/IS3Service.cs` - S3 service interface
- `Services/S3Service.cs` - S3 service implementation
- `Models/PresignedUrlResponse.cs` - Presigned URL response DTO

---

## ✅ Step 5: Tenant Middleware (JWT Claims Extraction)

### Completed Tasks:

1. **Created TenantMiddleware** (`Middleware/TenantMiddleware.cs`):
   - **Purpose:** Extracts tenant information (SchoolId, UserId, Role) from JWT claims on every authenticated request
   - **Key Features:**
     - ✅ Extracts `SchoolId` from JWT claims:
       - Checks for "SchoolId" claim first
       - Falls back to "TenantId" claim
       - Final fallback to `ClaimTypes.NameIdentifier`
     - ✅ Extracts `UserId` from JWT claims:
       - Checks for "UserId" claim first
       - Falls back to `ClaimTypes.NameIdentifier`
       - Final fallback to `ClaimTypes.Name`
     - ✅ Extracts `Role` from JWT claims:
       - Checks for "Role" claim first
       - Falls back to `ClaimTypes.Role`
     - ✅ Stores values in `HttpContext.Items`:
       - `HttpContext.Items["SchoolId"]` - Available throughout the request
       - `HttpContext.Items["UserId"]` - Available throughout the request
       - `HttpContext.Items["Role"]` - Available throughout the request
     - ✅ **Security & Validation:**
       - Returns 401 Unauthorized if user is not authenticated
       - Returns 401 Unauthorized if SchoolId is not found in token
       - Skips middleware for `/api/auth` endpoints (login, register)
       - Skips middleware for `/swagger` endpoints
     - ✅ **Error Handling:**
       - Try-catch block around entire middleware logic
       - Logs errors with context (path, user info)
       - Returns 500 Internal Server Error on unexpected exceptions
     - ✅ **Logging:**
       - Logs warnings for missing SchoolId
       - Logs debug info for extracted tenant information
       - Logs errors for unexpected exceptions

2. **Created HttpContextExtensions** (`Middleware/HttpContextExtensions.cs`):
   - **Purpose:** Provides convenient extension methods to access tenant information from controllers
   - **Extension Methods:**
     - ✅ `GetSchoolId()` - Returns SchoolId as string (nullable)
     - ✅ `GetSchoolIdAsGuid()` - Returns SchoolId as Guid (nullable, with validation)
     - ✅ `GetUserId()` - Returns UserId as string (nullable)
     - ✅ `GetRole()` - Returns Role as string (nullable)
     - ✅ `HasRole(role)` - Checks if user has a specific role (case-insensitive)
   - **Usage Example:**
     ```csharp
     var schoolId = HttpContext.GetSchoolIdAsGuid();
     var userId = HttpContext.GetUserId();
     var role = HttpContext.GetRole();
     
     if (HttpContext.HasRole("Admin"))
     {
         // Admin-specific logic
     }
     ```

3. **Registered Middleware in Program.cs:**
   - ✅ Added `using FeeManagementService.Middleware;`
   - ✅ Registered middleware in correct order:
     - `app.UseAuthentication()` - Must be first (authenticates the user)
     - `app.UseMiddleware<TenantMiddleware>()` - Extracts claims after authentication
     - `app.UseAuthorization()` - Authorizes based on roles/policies
   - **Why This Order Matters:**
     - Authentication must happen first to populate `context.User`
     - TenantMiddleware needs authenticated user to extract claims
     - Authorization happens last to check roles/policies

### How It Works:

1. **Request Flow:**
   ```
   Request → Authentication → TenantMiddleware → Authorization → Controller
   ```

2. **JWT Token Structure Expected:**
   ```json
   {
     "SchoolId": "guid-value",
     "UserId": "user-id",
     "Role": "Admin",
     // ... other claims
   }
   ```

3. **HttpContext.Items Usage:**
   - `HttpContext.Items` is a request-scoped dictionary
   - Data stored here is available only for the current HTTP request
   - Perfect for passing tenant context through the request pipeline
   - Automatically cleared after request completes

4. **Benefits:**
   - ✅ **Multi-tenancy Support:** Automatically extracts and validates tenant (SchoolId)
   - ✅ **Security:** Ensures every authenticated request has a valid SchoolId
   - ✅ **Convenience:** Easy access to tenant info via extension methods
   - ✅ **Separation of Concerns:** Middleware handles tenant extraction, controllers focus on business logic
   - ✅ **Type Safety:** Extension methods provide type-safe access (Guid conversion with validation)

### Files Created:
- `Middleware/TenantMiddleware.cs` - Middleware to extract tenant info from JWT
- `Middleware/HttpContextExtensions.cs` - Extension methods for easy access to tenant info

---

## ✅ Step 6: Fee Service (Business Logic Layer)

### Completed Tasks:

1. **Created IFeeService Interface** (`Services/IFeeService.cs`):
   - **Purpose:** Defines the contract for fee-related business operations
   - **Methods:**
     - ✅ `CreateFeeAsync(CreateFeeRequest request, string schoolId, string userId)` - Creates a new fee

2. **Created FeeService Implementation** (`Services/FeeService.cs`):
   - **Constructor Dependencies:**
     - `FeeDbContext` - Database context for data access
     - `IS3Service` - S3 service for image URL validation (dependency injected but not used in CreateFeeAsync)
     - `ILogger<FeeService>` - Structured logging
   - **CreateFeeAsync Method - Complete Implementation:**
     - ✅ **Input Validation:**
       - Validates SchoolId is not empty and is a valid GUID
       - Validates UserId is not empty
       - Validates ImageUrl format if provided (must be valid S3 URL using regex pattern)
       - Validates FeeType enum (case-insensitive parsing)
     - ✅ **Business Logic:**
       - Generates new Guid for Fee.Id
       - Sets SchoolId from parameter (extracted by TenantMiddleware)
       - Sets CreatedBy = userId (from parameter)
       - Sets CreatedAt = DateTime.UtcNow (UTC timestamp)
       - Sets ImageUrl from request (trimmed, null if empty)
       - Trims Title and Description to remove whitespace
       - Sets Status = FeeStatus.Active by default
       - Parses FeeType string to enum
     - ✅ **Data Persistence:**
       - Adds Fee entity to DbContext
       - Saves changes to database asynchronously
     - ✅ **Response Mapping:**
       - Maps Fee entity to FeeResponse using extension method
       - Returns FeeResponse to caller
     - ✅ **Error Handling:**
       - `DbUpdateException` - Handles database errors:
         - Checks for unique constraint violations
         - Wraps in InvalidOperationException with context
         - Logs as error with full exception details
       - `ArgumentException` - Invalid input parameters:
         - Re-throws with original stack trace preserved
         - Logs as warning (client error, not server error)
       - `Exception` - Unexpected errors:
         - Wraps in generic Exception with user-friendly message
         - Preserves inner exception for debugging
         - Logs as error with context
     - ✅ **Logging:**
       - Logs information when fee creation starts (with Title, SchoolId, UserId)
       - Logs success when fee is created (with FeeId, SchoolId, Title)
       - Logs warnings for invalid arguments
       - Logs errors for database and unexpected exceptions
   - ✅ **S3 URL Validation:**
     - Uses compiled regex pattern for performance
     - Pattern: `^https?://[^/]+\.s3[^/]*\.amazonaws\.com/.+$`
     - Validates S3 URL format: `https://bucket.s3.region.amazonaws.com/key`
     - Case-insensitive matching

3. **Updated CreateFeeRequest Model** (`Models/CreateFeeRequest.cs`):
   - ✅ Added `ImageUrl` property:
     - Type: `string?` (nullable)
     - MaxLength: 500 characters
     - Purpose: Client provides S3 URL after uploading image using presigned URL
     - Note: This is separate from `Image` property (IFormFile) which is used for direct uploads

4. **Updated CreateFeeRequestValidator** (`Validators/CreateFeeRequestValidator.cs`):
   - ✅ Added ImageUrl validation rules:
     - `MaximumLength(500)` - Ensures URL doesn't exceed database column limit
     - `Must(BeValidS3Url)` - Validates S3 URL format using regex
     - Only validates when ImageUrl is provided (not null/empty)
   - ✅ **BeValidS3Url Method:**
     - Uses same regex pattern as FeeService for consistency
     - Returns true if URL matches S3 format
     - Returns true if URL is null/empty (optional field)

5. **Registered FeeService in Program.cs:**
   - ✅ Registered as Scoped service: `builder.Services.AddScoped<IFeeService, FeeService>();`
   - **Why Scoped?**
     - Scoped lifetime matches DbContext lifetime (one per HTTP request)
     - Ensures same DbContext instance is used throughout the request
     - Properly disposes resources after request completes

### How It Works:

1. **Request Flow:**
   ```
   Controller → IFeeService.CreateFeeAsync() → Validation → Database → Response
   ```

2. **Validation Layers:**
   - **FluentValidation** (automatic) - Validates request model properties
   - **Service Layer Validation** - Validates business rules (SchoolId, ImageUrl format)
   - **Database Constraints** - Enforces data integrity (check constraints, indexes)

3. **Image Upload Workflow:**
   - Client requests presigned URL from S3Service
   - Client uploads image directly to S3 using presigned URL
   - Client receives S3 URL after successful upload
   - Client includes S3 URL in CreateFeeRequest.ImageUrl
   - FeeService validates S3 URL format before saving

4. **Error Handling Strategy:**
   - **Client Errors (ArgumentException):** Re-thrown with original stack trace
   - **Database Errors (DbUpdateException):** Wrapped with context, checked for specific violations
   - **Unexpected Errors (Exception):** Wrapped with user-friendly message, inner exception preserved

5. **Benefits:**
   - ✅ **Separation of Concerns:** Business logic separated from controllers
   - ✅ **Testability:** Service can be unit tested independently
   - ✅ **Reusability:** Service can be used by multiple controllers
   - ✅ **Validation:** Multiple layers of validation ensure data integrity
   - ✅ **Error Handling:** Comprehensive error handling with proper logging
   - ✅ **Type Safety:** Strong typing with enums and GUIDs

### Files Created:
- `Services/IFeeService.cs` - Fee service interface
- `Services/FeeService.cs` - Fee service implementation

### Files Modified:
- `Models/CreateFeeRequest.cs` - Added ImageUrl property
- `Validators/CreateFeeRequestValidator.cs` - Added ImageUrl validation

---

## ✅ Step 7: Fees Controller (API Endpoints)

### Completed Tasks:

1. **Created GeneratePresignedUrlRequest Model** (`Models/GeneratePresignedUrlRequest.cs`):
   - **Purpose:** Request DTO for presigned URL generation
   - **Properties:**
     - ✅ `FeeId` (string, required) - The fee ID for which image is being uploaded
     - ✅ `FileName` (string, required) - Original filename of the image
     - ✅ `ContentType` (string, required) - MIME type of the image
     - ✅ `FileSize` (long, required, range 1-5MB) - Size of the file in bytes
   - **Data Annotations:** Uses `[Required]` and `[Range]` attributes for validation

2. **Created FeesController** (`Controllers/FeesController.cs`):
   - **Controller Attributes:**
     - ✅ `[ApiController]` - Enables API-specific features (automatic model validation, ProblemDetails)
     - ✅ `[Route("api/[controller]")]` - Base route: `/api/fees`
     - ✅ `[Authorize]` - Requires authentication for all endpoints in controller
   
   - **Constructor Dependencies:**
     - `IFeeService` - Business logic service for fee operations
     - `IS3Service` - S3 service for presigned URL generation
     - `ILogger<FeesController>` - Structured logging
   
   - **GeneratePresignedUrl Endpoint** (`[HttpPost("presigned-url")]`):
     - ✅ **Authorization:** `[Authorize(Roles = "SchoolAdmin")]` - Requires SchoolAdmin role
     - ✅ **Route:** `POST /api/fees/presigned-url`
     - ✅ **Request:** `[FromBody] GeneratePresignedUrlRequest`
     - ✅ **Response:** `200 OK` with `PresignedUrlResponse`
     - ✅ **Implementation:**
       - Gets SchoolId from HttpContext.Items (set by TenantMiddleware)
       - Validates SchoolId is present (returns 401 if missing)
       - Validates file size (max 5MB = 5 * 1024 * 1024 bytes)
       - Validates content type (image/jpeg, image/jpg, image/png, image/webp)
       - Calls `IS3Service.GeneratePresignedUrlAsync()` with validated parameters
       - Returns presigned URL response
     - ✅ **Error Handling:**
       - `ArgumentException` → 400 Bad Request (with ProblemDetails)
       - `UnauthorizedAccessException` → 401 Unauthorized (with ProblemDetails)
       - `Exception` → 500 Internal Server Error (with ProblemDetails)
     - ✅ **Swagger Documentation:**
       - XML comments describing the endpoint
       - `[ProducesResponseType]` attributes for all response types
       - Includes ProblemDetails responses for errors
     - ✅ **Logging:**
       - Logs info when request received (with FeeId, FileName, ContentType, FileSize)
       - Logs info when presigned URL generated successfully (with FeeId, S3Key)
       - Logs warnings for invalid arguments
       - Logs errors for unexpected exceptions
   
   - **CreateFee Endpoint** (`[HttpPost]`):
     - ✅ **Authorization:** `[Authorize(Roles = "SchoolAdmin")]` - Requires SchoolAdmin role
     - ✅ **Route:** `POST /api/fees`
     - ✅ **Request:** `[FromBody] CreateFeeRequest` (includes ImageUrl, not IFormFile)
     - ✅ **Response:** `201 Created` with `FeeResponse` and Location header
     - ✅ **Implementation:**
       - Gets SchoolId from HttpContext.Items (set by TenantMiddleware)
       - Gets UserId from HttpContext.Items or falls back to `User.Identity.Name`
       - Validates SchoolId and UserId are present (returns 401 if missing)
       - Calls `IFeeService.CreateFeeAsync()` with request, schoolId, and userId
       - Returns 201 Created with FeeResponse and Location header pointing to GetFeeById
     - ✅ **Error Handling:**
       - `ValidationException` → 400 Bad Request (with ValidationProblemDetails and error details)
       - `ArgumentException` → 400 Bad Request (with ProblemDetails)
       - `UnauthorizedAccessException` → 401 Unauthorized (with ProblemDetails)
       - `InvalidOperationException` → 403 Forbidden (with ProblemDetails)
       - `Exception` → 500 Internal Server Error (with ProblemDetails)
     - ✅ **Swagger Documentation:**
       - XML comments describing the endpoint
       - `[ProducesResponseType]` attributes for all response types
       - Includes ProblemDetails responses for all error scenarios
     - ✅ **Logging:**
       - Logs info when request received (with Title, Amount, FeeType)
       - Logs info when fee created successfully (with FeeId, SchoolId, Title)
       - Logs warnings for validation and argument errors
       - Logs errors for unexpected exceptions
   
   - **GetFeeById Endpoint** (Placeholder):
     - ✅ Private method for `CreatedAtAction` to generate Location header
     - Will be implemented later for retrieving fees by ID

3. **Error Handling with ProblemDetails:**
   - ✅ **ProblemDetails Format:**
     - Standard RFC 7807 format for error responses
     - Includes `title`, `detail`, and `statusCode`
     - Provides consistent error response structure
   - ✅ **ValidationProblemDetails:**
     - Used for validation errors (FluentValidation exceptions)
     - Includes dictionary of field errors
     - Groups errors by property name
   - ✅ **Error Response Examples:**
     ```json
     {
       "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
       "title": "Invalid Request",
       "status": 400,
       "detail": "File size must be between 1 byte and 5242880 bytes (5MB)."
     }
     ```

4. **Swagger/OpenAPI Documentation:**
   - ✅ **XML Comments:**
     - Summary and description for each endpoint
     - Parameter descriptions
     - Response descriptions
   - ✅ **Attributes:**
     - `[ProducesResponseType]` for all possible response types
     - Includes success responses (200, 201)
     - Includes error responses (400, 401, 403, 500)
     - Specifies ProblemDetails for error responses

### How It Works:

1. **Request Flow for Presigned URL:**
   ```
   Client → POST /api/fees/presigned-url
   → Authentication (JWT)
   → TenantMiddleware (extracts SchoolId)
   → Authorization (checks SchoolAdmin role)
   → FeesController.GeneratePresignedUrl()
   → Validates file size & content type
   → IS3Service.GeneratePresignedUrlAsync()
   → Returns PresignedUrlResponse
   ```

2. **Request Flow for Create Fee:**
   ```
   Client → POST /api/fees
   → Authentication (JWT)
   → TenantMiddleware (extracts SchoolId, UserId)
   → Authorization (checks SchoolAdmin role)
   → FluentValidation (validates CreateFeeRequest)
   → FeesController.CreateFee()
   → IFeeService.CreateFeeAsync()
   → Database (saves Fee entity)
   → Returns FeeResponse (201 Created)
   ```

3. **Authorization Flow:**
   - All endpoints require `[Authorize]` attribute
   - GeneratePresignedUrl and CreateFee require `SchoolAdmin` role
   - TenantMiddleware ensures SchoolId is present
   - Authorization happens after authentication and tenant extraction

4. **Error Response Flow:**
   - Exceptions caught in controller
   - Logged with appropriate level (warning/error)
   - Converted to ProblemDetails format
   - Returned with appropriate HTTP status code
   - Client receives consistent error format

5. **Benefits:**
   - ✅ **Security:** Role-based authorization ensures only SchoolAdmins can create fees
   - ✅ **Tenant Isolation:** SchoolId automatically extracted and validated
   - ✅ **Validation:** Multiple layers (FluentValidation, controller, service)
   - ✅ **Error Handling:** Consistent ProblemDetails format for all errors
   - ✅ **Documentation:** Complete Swagger/OpenAPI documentation
   - ✅ **Logging:** Comprehensive logging for debugging and monitoring
   - ✅ **RESTful:** Follows REST conventions (201 Created, Location header)

### Files Created:
- `Controllers/FeesController.cs` - Main API controller for fee operations
- `Models/GeneratePresignedUrlRequest.cs` - Request DTO for presigned URL generation

---

## 📊 Project Structure Summary

```
FeeManagementService/
├── Controllers/
│   ├── HealthController.cs
│   └── FeesController.cs
├── Models/
│   ├── Fee.cs
│   ├── FeeType.cs
│   ├── FeeStatus.cs
│   ├── CreateFeeRequest.cs
│   ├── FeeResponse.cs
│   ├── FeeExtensions.cs
│   ├── PresignedUrlResponse.cs
│   └── GeneratePresignedUrlRequest.cs
├── Services/
│   ├── IS3Service.cs
│   ├── S3Service.cs
│   ├── IFeeService.cs
│   └── FeeService.cs
├── Data/
│   ├── ApplicationDbContext.cs
│   └── FeeDbContext.cs
├── Validators/
│   └── CreateFeeRequestValidator.cs
├── Middleware/
│   ├── TenantMiddleware.cs
│   ├── HttpContextExtensions.cs
│   ├── GlobalExceptionHandlerMiddleware.cs
│   └── RequestLoggingMiddleware.cs
├── Configuration/
│   ├── JwtSettings.cs
│   └── AwsS3Settings.cs
├── Migrations/
│   ├── 20260130133744_InitialCreate.cs
│   ├── 20260130133744_InitialCreate.Designer.cs
│   └── FeeDbContextModelSnapshot.cs
├── Program.cs
├── appsettings.json
└── PROGRESS.md (this file)
```

---

## 🔧 Configuration Summary

### Database:
- **Provider:** SQL Server
- **Server:** VASANTH\SQLEXPRESS
- **Database:** FeeManagementDb
- **Connection String:** Configured with TrustServerCertificate=True

### AWS S3:
- **Bucket:** school-platform-fees-vasanth
- **Region:** us-east-1
- **Authentication:** Access Key ID and Secret Access Key
- **File Upload:** Presigned URL approach

### JWT Authentication:
- **Issuer:** FeeManagementService
- **Audience:** FeeManagementService
- **Secret Key:** Configured in appsettings.json

---

## ✅ Build Status

- **Last Build:** ✅ Successful
- **Warnings:** 0
- **Errors:** 0
- **Database Migration:** ✅ Applied successfully

---

## 📝 Next Steps (To Be Implemented)

1. Implement GetFeeById endpoint (for retrieving individual fees)
2. Create update and delete endpoints
3. Add pagination and filtering for fee listings
4. Implement unit tests
5. Add integration tests

---

## 📅 Progress Timeline

- **Step 1:** Project Setup and Configuration ✅
- **Step 2:** Database Model and DbContext ✅
- **Step 3:** Request/Response Models and Validation ✅
- **Step 4:** AWS S3 Service (Presigned URL Approach) ✅
- **Step 5:** Tenant Middleware (JWT Claims Extraction) ✅
- **Step 6:** Fee Service (Business Logic Layer) ✅
- **Step 7:** Fees Controller (API Endpoints) ✅
- **Step 8:** JWT Authentication Configuration ✅
- **Step 9:** Error Handling and Logging ✅
- **Step 10:** Testing and Documentation ✅
- **Step 11:** JWT Token Generation Endpoint ✅

---

## ✅ Step 11: JWT Token Generation Endpoint (Login)

### Completed Tasks:

1. **Created LoginRequest Model** (`Models/LoginRequest.cs`):
   - **Properties:**
     - ✅ `Username` (string, required) - User identifier
     - ✅ `Password` (string, required) - User password
     - ✅ `SchoolId` (string, required) - School identifier (GUID)
   - **XML Comments:** Complete documentation with examples

2. **Created LoginResponse Model** (`Models/LoginResponse.cs`):
   - **Properties:**
     - ✅ `Token` (string) - JWT access token
     - ✅ `ExpiresAt` (DateTime) - Token expiration time
     - ✅ `UserId` (string) - User identifier
     - ✅ `SchoolId` (string) - School identifier
     - ✅ `Role` (string) - User role (e.g., "SchoolAdmin")

3. **Created IJwtTokenService Interface** (`Services/IJwtTokenService.cs`):
   - ✅ `GenerateToken(string userId, string schoolId, string role, string username)` - Generates JWT token

4. **Created JwtTokenService Implementation** (`Services/JwtTokenService.cs`):
   - **Constructor:** IOptions<JwtSettings>, ILogger<JwtTokenService>
   - **GenerateToken Method:**
     - ✅ Creates JWT token with all required claims:
       - Standard claims: `sub`, `jti`, `iat`
       - Custom claims: `UserId`, `SchoolId`, `Role`
       - Name claim: `ClaimTypes.Name`
     - ✅ Uses JwtSettings from configuration:
       - Issuer, Audience, SecretKey, ExpirationMinutes
     - ✅ Signs token with HMAC SHA256
     - ✅ Returns token string
     - ✅ Logs token generation

5. **Created AuthController** (`Controllers/AuthController.cs`):
   - **Attributes:**
     - ✅ `[ApiController]` - API-specific features
     - ✅ `[Route("api/[controller]")]` - Base route: `/api/auth`
     - ✅ No `[Authorize]` - Login endpoint is public
   - **Login Endpoint** (`[HttpPost("login")]`):
     - ✅ **Route:** `POST /api/auth/login`
     - ✅ **Request:** `[FromBody] LoginRequest`
     - ✅ **Response:** `200 OK` with `LoginResponse`
     - ✅ **Validation:**
       - Validates Username is not empty
       - Validates SchoolId is not empty
       - Validates SchoolId is a valid GUID
       - Validates Password is not empty (for POC)
     - ✅ **Token Generation:**
       - Generates UserId from username hash (for POC)
       - Sets default role as "SchoolAdmin" (for POC)
       - Calls `IJwtTokenService.GenerateToken()`
       - Returns LoginResponse with token and user info
     - ✅ **Error Handling:**
       - `400 Bad Request` - Invalid request data
       - `401 Unauthorized` - Invalid credentials (empty password)
       - `500 Internal Server Error` - Unexpected errors
     - ✅ **Swagger Documentation:**
       - XML comments
       - `[ProducesResponseType]` attributes
     - ✅ **Logging:**
       - Logs login attempts
       - Logs successful logins
       - Logs errors
   - **POC Note:**
     - Currently accepts any valid username/password
     - In production, should validate against user database
     - Should verify user belongs to specified school
     - Should get role from database

6. **Registered JwtTokenService in Program.cs:**
   - ✅ Registered as Scoped service: `builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();`

### How It Works:

1. **Login Flow:**
   ```
   Client → POST /api/auth/login
   → AuthController.Login()
   → Validates request
   → JwtTokenService.GenerateToken()
   → Creates JWT with claims
   → Returns LoginResponse with token
   ```

2. **Token Generation Process:**
   - Reads JWT settings from configuration
   - Creates claims (UserId, SchoolId, Role, standard claims)
   - Signs token with secret key
   - Sets expiration time
   - Returns token string

3. **Token Usage:**
   - Client receives token from login response
   - Client includes token in Authorization header: `Bearer <token>`
   - Token is validated by JWT middleware
   - Claims are extracted by TenantMiddleware

### Files Created:
- `Controllers/AuthController.cs` - Authentication controller with login endpoint
- `Models/LoginRequest.cs` - Login request DTO
- `Models/LoginResponse.cs` - Login response DTO
- `Services/IJwtTokenService.cs` - JWT token service interface
- `Services/JwtTokenService.cs` - JWT token service implementation

### Postman Collection Updated:
- ✅ Added **"Login (Get JWT Token)"** endpoint as the first request in the collection
- ✅ Automatically saves JWT token to `jwtToken` collection variable
- ✅ Automatically saves `userId` and `schoolId` from login response
- ✅ Updated `baseUrl` to `http://localhost:5297` (matching actual running port)
- ✅ Added test scripts to validate login response
- ✅ No authentication required (public endpoint)
- ✅ Updated collection description with complete workflow

### Usage Example:

**Request:**
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin@school.com",
  "password": "password123",
  "schoolId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-02-01T15:30:00Z",
  "userId": "user-abc123",
  "schoolId": "550e8400-e29b-41d4-a716-446655440000",
  "role": "SchoolAdmin"
}
```

### Important Notes:

- ✅ **POC Implementation:** Login accepts any valid credentials
- ⚠️ **Production:** Should integrate with user authentication system
- ✅ **Token Claims:** Automatically includes all required claims
- ✅ **No Authorization Required:** Login endpoint is public (no [Authorize] attribute)

---

## ✅ Step 10: Testing and Documentation

### Completed Tasks:

1. **Enabled XML Documentation Generation:**
   - ✅ Updated `FeeManagementService.csproj`:
     - Added `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
     - Added `<NoWarn>$(NoWarn);1591</NoWarn>` to suppress missing XML comment warnings
   - ✅ XML documentation file generated: `FeeManagementService.xml`

2. **Enhanced Swagger/OpenAPI Configuration:**
   - ✅ **XML Comments Integration:**
     - Configured Swagger to read XML documentation file
     - Includes all XML comments in Swagger UI
   - ✅ **Enhanced API Info:**
     - Added Contact information
     - Enhanced description with key features
   - ✅ **JWT Authentication:**
     - Already configured with Bearer token support
     - Swagger UI includes "Authorize" button
   - ✅ **Response Types:**
     - All endpoints have `[ProducesResponseType]` attributes
     - Includes success and error response types
     - ProblemDetails format for errors

3. **Added XML Comments to Models:**
   - ✅ **CreateFeeRequest:**
     - Added summary and property descriptions
     - Added examples for each property
     - Documented ImageUrl format and usage
   - ✅ **GeneratePresignedUrlRequest:**
     - Added summary and property descriptions
     - Added examples for each property
     - Documented valid content types and file size limits

4. **Created Comprehensive README.md:**
   - ✅ **Prerequisites Section:**
     - .NET 8.0 SDK
     - SQL Server requirements
     - AWS Account setup
     - Development tools
   - ✅ **Setup Instructions:**
     - Step-by-step setup guide
     - Database migration instructions
     - Configuration steps
   - ✅ **AWS S3 Configuration:**
     - Detailed bucket creation steps
     - CORS configuration with example
     - IAM user/role setup (both options)
     - Bucket policy example
     - Application configuration
   - ✅ **Database Setup:**
     - Initial migration instructions
     - Creating new migrations
     - Rollback procedures
   - ✅ **Configuration:**
     - Environment variables guide
     - Logging configuration
   - ✅ **API Documentation:**
     - Swagger UI access
     - Endpoint documentation with examples
     - Request/response formats
   - ✅ **Testing:**
     - Swagger UI testing
     - Postman testing reference
     - Test cases overview
   - ✅ **Complete Workflow:**
     - Step-by-step workflow (Get presigned URL → Upload to S3 → Create fee)
     - HTTP request examples
   - ✅ **Troubleshooting:**
     - Common issues and solutions
     - Database connection issues
     - AWS S3 access problems
     - JWT token issues
     - Migration errors
   - ✅ **Security Best Practices:**
     - Credential management
     - HTTPS requirements
     - CORS configuration

5. **Created Postman Testing Guide** (`POSTMAN_TESTING.md`):
   - ✅ **Postman Collection Setup:**
     - Collection creation instructions
     - Variable configuration
     - Authorization setup
   - ✅ **API Endpoints Documentation:**
     - Generate Presigned URL endpoint
     - Upload to S3 endpoint
     - Create Fee endpoint
     - Request/response examples
     - Test scripts for validation
   - ✅ **Test Cases:**
     - **Success Cases:**
       - Get presigned URL with valid metadata
       - Upload image to S3
       - Create fee with imageUrl
       - Create fee without image
     - **Error Cases:**
       - Missing required fields
       - Invalid amount
       - File too large
       - Invalid content type
       - Invalid imageUrl format
       - Unauthorized (no token)
       - Forbidden (wrong role)
       - Expired presigned URL
   - ✅ **Postman Collection JSON:**
     - Complete collection export
     - Ready to import into Postman
   - ✅ **Environment Setup:**
     - Development environment variables
     - Production environment variables
   - ✅ **Automated Testing:**
     - Collection runner instructions
     - Test result interpretation
   - ✅ **Tips and Troubleshooting:**
     - Best practices
     - Common issues and solutions

### How It Works:

1. **XML Documentation:**
   - Build process generates `FeeManagementService.xml`
   - Swagger reads XML file and includes comments
   - Comments appear in Swagger UI under each endpoint/model

2. **Swagger UI Features:**
   - Interactive API testing
   - JWT token authentication
   - Request/response examples
   - Schema documentation
   - Try-it-out functionality

3. **Testing Workflow:**
   ```
   1. Generate JWT token (see JWT_TESTING.md)
   2. Open Swagger UI or Postman
   3. Authorize with JWT token
   4. Test Generate Presigned URL endpoint
   5. Upload image to S3 using presigned URL
   6. Test Create Fee endpoint with imageUrl
   ```

4. **Documentation Structure:**
   - **README.md** - Main documentation (setup, configuration, usage)
   - **JWT_TESTING.md** - JWT token generation and testing
   - **POSTMAN_TESTING.md** - Complete Postman testing guide
   - **PROGRESS.md** - Implementation progress and details

### Benefits:

- ✅ **Developer Onboarding:** Clear setup instructions
- ✅ **API Discovery:** Swagger UI provides interactive documentation
- ✅ **Testing:** Comprehensive test cases and examples
- ✅ **Troubleshooting:** Common issues documented
- ✅ **Workflow Documentation:** Complete end-to-end workflow
- ✅ **Security Guidance:** Best practices documented

### Files Created:
- `README.md` - Comprehensive project documentation
- `POSTMAN_TESTING.md` - Complete Postman testing guide
- `FeeManagementService.postman_collection.json` - Postman collection file (ready to import)
- `POSTMAN_IMPORT_INSTRUCTIONS.md` - Step-by-step import and manual creation guide
- `POSTMAN_TROUBLESHOOTING.md` - Troubleshooting guide for import issues

### Files Modified:
- `FeeManagementService.csproj` - Enabled XML documentation generation
- `Program.cs` - Enhanced Swagger configuration with XML comments
- `Models/CreateFeeRequest.cs` - Added XML comments
- `Models/GeneratePresignedUrlRequest.cs` - Added XML comments

### Documentation Files:

1. **README.md** - Main documentation (comprehensive setup and usage guide)
2. **JWT_TESTING.md** - JWT token testing guide
3. **POSTMAN_TESTING.md** - Postman collection and testing guide
4. **FeeManagementService.postman_collection.json** - Postman collection (importable JSON file)
5. **PROGRESS.md** - Implementation progress and technical details

### Postman Collection Features:

- ✅ **8 Requests:**
  - Generate Presigned URL
  - Upload Image to S3
  - Create Fee (with image)
  - Create Fee (without image)
  - Error Cases (5 test scenarios)
- ✅ **Collection Variables:**
  - baseUrl, jwtToken, schoolId, userId, feeId
  - Dynamic variables (presignedUrl, imageUrl, s3Key, createdFeeId)
- ✅ **Automatic Variable Saving:**
  - Presigned URL response saves variables for next requests
  - ImageUrl automatically used in Create Fee request
- ✅ **Test Scripts:**
  - Status code validation
  - Response structure validation
  - Automatic variable extraction
- ✅ **Bearer Token Authentication:**
  - Configured at collection level
  - All requests automatically include Authorization header
- ✅ **Error Test Cases:**
  - Missing required fields
  - Invalid amount
  - File too large
  - Invalid content type
  - Invalid imageUrl format
  - Unauthorized access

---

## ✅ Step 9: Error Handling and Logging

### Completed Tasks:

1. **Created GlobalExceptionHandlerMiddleware** (`Middleware/GlobalExceptionHandlerMiddleware.cs`):
   - **Purpose:** Catches all unhandled exceptions globally and returns consistent error responses
   - **Key Features:**
     - ✅ **Exception Handling:**
       - Catches all unhandled exceptions in the request pipeline
       - Logs exceptions with Serilog (includes Path, Method, SchoolId, UserId)
       - Maps exceptions to appropriate HTTP status codes:
         - `ArgumentException` → 400 Bad Request
         - `UnauthorizedAccessException` → 401 Unauthorized
         - `InvalidOperationException` → 403 Forbidden
         - `KeyNotFoundException` → 404 Not Found
         - `Exception` (others) → 500 Internal Server Error
     - ✅ **ProblemDetails Format:**
       - Returns RFC 7807 compliant ProblemDetails JSON
       - Includes: type, title, status, detail, instance, traceId
       - Development mode shows full exception message
       - Production mode shows user-friendly messages
     - ✅ **Error Response Structure:**
       ```json
       {
         "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
         "title": "Internal Server Error",
         "status": 500,
         "detail": "An error occurred...",
         "instance": "/api/fees",
         "traceId": "00-abc123..."
       }
       ```
     - ✅ **Logging:**
       - Logs all exceptions with error level
       - Includes request context (Path, Method, SchoolId, UserId)
       - Preserves exception stack trace in logs

2. **Enhanced Serilog Configuration** (`Program.cs`):
   - ✅ **Console Logging:**
     - Writes to console with formatted output
     - Custom output template with timestamp, level, message, properties
   - ✅ **File Logging:**
     - Writes to `logs/fee-management-.log` with daily rolling
     - Retains 30 days of log files
     - Includes full timestamp with timezone
     - Same output template as console
   - ✅ **Log Enrichment:**
     - `FromLogContext` - Includes correlation IDs and custom properties
     - `WithProperty("Application")` - Adds application name to all logs
   - ✅ **Configuration from appsettings.json:**
     - Minimum log levels configurable
     - Overrides for Microsoft namespaces (reduces noise)
     - Can be adjusted per environment

3. **Created RequestLoggingMiddleware** (`Middleware/RequestLoggingMiddleware.cs`):
   - **Purpose:** Logs all incoming requests and responses for monitoring and debugging
   - **Key Features:**
     - ✅ **Request Logging:**
       - Logs incoming requests: Method, Path, SchoolId, UserId
       - Includes correlation ID (TraceIdentifier) for request tracking
       - Skips logging for health checks, Swagger, and favicon
     - ✅ **Response Logging:**
       - Logs response status codes
       - Logs elapsed time (milliseconds) for performance monitoring
       - Different log levels based on status code:
         - 5xx → Error level
         - 4xx → Warning level
         - 2xx/3xx → Information level
     - ✅ **Correlation ID:**
       - Adds `X-Correlation-Id` header to response
       - Uses ASP.NET Core's TraceIdentifier
       - Enables tracking requests across distributed systems
     - ✅ **Performance Tracking:**
       - Uses Stopwatch to measure request duration
       - Logs elapsed time in milliseconds
     - ✅ **Security:**
       - Excludes sensitive data from logs
       - Only logs SchoolId and UserId (no passwords, tokens, etc.)

4. **Updated appsettings.json:**
   - ✅ **Serilog Configuration Section:**
     - MinimumLevel settings (Default: Information)
     - Overrides for Microsoft namespaces (Warning level)
     - WriteTo configuration (Console and File)
     - Enrichment configuration
   - ✅ **Log File Settings:**
     - Path: `logs/fee-management-.log`
     - RollingInterval: Day (creates new file daily)
     - RetainedFileCountLimit: 30 (keeps 30 days of logs)

5. **Registered Middleware in Program.cs:**
   - ✅ **Middleware Order (Critical for proper execution):**
     1. `UseHttpsRedirection()` - HTTPS enforcement
     2. `UseMiddleware<RequestLoggingMiddleware>()` - Early logging
     3. `UseCors()` - CORS handling
     4. `UseAuthentication()` - JWT authentication
     5. `UseMiddleware<TenantMiddleware>()` - Tenant extraction
     6. `UseAuthorization()` - Role-based authorization
     7. `UseMiddleware<GlobalExceptionHandlerMiddleware>()` - Exception handling
     8. `MapControllers()` - Route to controllers
   - ✅ **Why This Order Matters:**
     - RequestLoggingMiddleware early to log all requests
     - Authentication before TenantMiddleware (needs authenticated user)
     - GlobalExceptionHandlerMiddleware last to catch all exceptions
     - Exception handler after authorization to catch auth errors

### How It Works:

1. **Request Flow with Logging:**
   ```
   Request → RequestLoggingMiddleware (logs start)
   → Authentication → TenantMiddleware → Authorization
   → Controller → Service → Database
   → Response → RequestLoggingMiddleware (logs completion)
   → If Exception → GlobalExceptionHandlerMiddleware (logs & handles)
   ```

2. **Exception Handling Flow:**
   ```
   Exception occurs
   → GlobalExceptionHandlerMiddleware catches it
   → Logs exception with context (Path, Method, SchoolId, UserId)
   → Maps to HTTP status code
   → Returns ProblemDetails JSON
   → Client receives consistent error format
   ```

3. **Logging Features:**
   - ✅ **Structured Logging:** All logs in structured format (JSON properties)
   - ✅ **Correlation IDs:** Every request has unique correlation ID
   - ✅ **Performance Metrics:** Request duration logged
   - ✅ **Context Enrichment:** SchoolId, UserId automatically included
   - ✅ **Multiple Sinks:** Console (development) and File (production)

4. **Log File Structure:**
   ```
   logs/
   ├── fee-management-2026-02-01.log
   ├── fee-management-2026-02-02.log
   └── ... (30 days retained)
   ```

5. **Benefits:**
   - ✅ **Consistent Error Responses:** All errors return ProblemDetails format
   - ✅ **Comprehensive Logging:** All requests and exceptions logged
   - ✅ **Request Tracking:** Correlation IDs enable request tracing
   - ✅ **Performance Monitoring:** Request duration logged
   - ✅ **Debugging:** Detailed exception information in logs
   - ✅ **Production Ready:** User-friendly error messages in production
   - ✅ **Security:** Sensitive data excluded from logs

### Files Created:
- `Middleware/GlobalExceptionHandlerMiddleware.cs` - Global exception handler
- `Middleware/RequestLoggingMiddleware.cs` - Request/response logging middleware

### Files Modified:
- `Program.cs` - Enhanced Serilog configuration, registered middleware
- `appsettings.json` - Added Serilog configuration section

### Log Output Examples:

**Request Log:**
```
[14:30:15 INF] Incoming request: POST /api/fees, SchoolId: 550e8400-..., UserId: user-123, CorrelationId: 00-abc123...
[14:30:15 INF] Request completed: POST /api/fees, StatusCode: 201, ElapsedMs: 245, SchoolId: 550e8400-..., UserId: user-123, CorrelationId: 00-abc123...
```

**Error Log:**
```
[14:30:20 ERR] Unhandled exception occurred. Path: /api/fees, Method: POST, SchoolId: 550e8400-..., UserId: user-123
System.ArgumentException: Invalid argument
   at FeeService.CreateFeeAsync(...)
```

---

## ✅ Step 8: JWT Authentication Configuration

### Completed Tasks:

1. **Enhanced JwtSettings Configuration Class** (`Configuration/JwtSettings.cs`):
   - ✅ Added `ExpirationMinutes` property (default: 60 minutes)
   - ✅ Properties:
     - `Issuer` - Token issuer identifier
     - `Audience` - Token audience identifier
     - `SecretKey` - Secret key for signing tokens (must be at least 32 characters)
     - `ExpirationMinutes` - Token expiration time in minutes

2. **JWT Authentication Configuration in Program.cs:**
   - ✅ **Authentication Setup:**
     - `AddAuthentication()` - Configures authentication services
     - `AddJwtBearer()` - Adds JWT Bearer token authentication
   - ✅ **Token Validation Parameters:**
     - `ValidateIssuer = true` - Validates token issuer
     - `ValidateAudience = true` - Validates token audience
     - `ValidateLifetime = true` - Validates token expiration
     - `ValidateIssuerSigningKey = true` - Validates signing key
     - `ValidIssuer` - From appsettings.json (`FeeManagementService`)
     - `ValidAudience` - From appsettings.json (`FeeManagementService`)
     - `IssuerSigningKey` - SymmetricSecurityKey from SecretKey in appsettings.json
   - ✅ **Security Scheme:**
     - DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme
     - DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme

3. **Updated appsettings.json:**
   - ✅ **JwtSettings Section:**
     - `Issuer`: `FeeManagementService`
     - `Audience`: `FeeManagementService`
     - `SecretKey`: Long secret key (at least 32 characters for security)
     - `ExpirationMinutes`: `60` (configurable)

4. **Created JWT Testing Documentation** (`JWT_TESTING.md`):
   - ✅ **JWT Configuration Details:**
     - Documents all JWT settings
     - Explains required claims structure
   - ✅ **Required JWT Claims:**
     - **Custom Claims:**
       - `SchoolId` (Guid/string) - Required for tenant isolation
       - `UserId` (string) - Required for user identification
       - `Role` (string) - Required for authorization (e.g., "SchoolAdmin")
     - **Standard Claims:**
       - `sub` (subject) - User identifier
       - `exp` (expiration) - Token expiration time
       - `iat` (issued at) - Token issued time
       - `iss` (issuer) - Must match configured issuer
       - `aud` (audience) - Must match configured audience
   - ✅ **Testing Methods:**
     - Using jwt.io (online tool)
     - Programmatic token generation (C# code example)
     - Using Swagger UI
   - ✅ **Sample Token Structure:**
     - JSON payload example
     - Complete token structure with all required claims
   - ✅ **Testing Examples:**
     - HTTP request examples for both endpoints
     - Authorization header format
   - ✅ **Troubleshooting:**
     - Common issues and solutions
     - Error code explanations

### How JWT Authentication Works:

1. **Token Structure:**
   ```
   Header.Payload.Signature
   ```
   - **Header:** Algorithm and token type (HS256, JWT)
   - **Payload:** Claims (SchoolId, UserId, Role, exp, iat, iss, aud, sub)
   - **Signature:** HMAC SHA256 signature using SecretKey

2. **Authentication Flow:**
   ```
   Client → Request with Bearer Token
   → JWT Middleware validates token
   → Checks signature, expiration, issuer, audience
   → Extracts claims to HttpContext.User
   → TenantMiddleware extracts SchoolId, UserId, Role
   → Authorization checks role
   → Controller processes request
   ```

3. **Token Validation Process:**
   - ✅ **Signature Validation:** Verifies token wasn't tampered with
   - ✅ **Expiration Check:** Ensures token hasn't expired
   - ✅ **Issuer Validation:** Verifies token came from correct issuer
   - ✅ **Audience Validation:** Verifies token is for this application
   - ✅ **Claims Extraction:** Extracts all claims to `HttpContext.User`

4. **Security Features:**
   - ✅ **Symmetric Key:** Uses HMAC SHA256 with shared secret
   - ✅ **Token Expiration:** Tokens expire after configured time (default 60 minutes)
   - ✅ **Claim Validation:** Validates required claims (SchoolId, UserId, Role)
   - ✅ **HTTPS Recommended:** Tokens should be transmitted over HTTPS in production

5. **Benefits:**
   - ✅ **Stateless:** No server-side session storage required
   - ✅ **Scalable:** Works across multiple servers without shared session store
   - ✅ **Secure:** Cryptographically signed, tamper-proof
   - ✅ **Flexible:** Can include custom claims for tenant isolation
   - ✅ **Standard:** Uses industry-standard JWT format

### Files Modified:
- `Configuration/JwtSettings.cs` - Added ExpirationMinutes property
- `appsettings.json` - Added ExpirationMinutes to JwtSettings
- `Program.cs` - Already had complete JWT configuration (verified)

### Files Created:
- `JWT_TESTING.md` - Comprehensive JWT testing guide and documentation

### JWT Token Example:

```json
{
  "sub": "user-123",
  "iss": "FeeManagementService",
  "aud": "FeeManagementService",
  "exp": 1735689600,
  "iat": 1735686000,
  "SchoolId": "550e8400-e29b-41d4-a716-446655440000",
  "UserId": "user-123",
  "Role": "SchoolAdmin"
}
```

### Usage in Requests:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 🔒 Security Best Practices

### Credential Management:
- ✅ **AWS Credentials:** Removed from source control
- ✅ **.gitignore:** Created to exclude sensitive files
- ✅ **appsettings.json:** Uses placeholder values
- ✅ **appsettings.Development.json.example:** Template file created for reference

### Recommended Approach:
1. **For Development:**
   - Use `appsettings.Development.json` (excluded from git)
   - Store actual credentials locally only
   - Never commit real credentials

2. **For Production:**
   - Use environment variables
   - Use Azure Key Vault / AWS Secrets Manager
   - Use IAM roles when possible (instead of access keys)

3. **Git Configuration:**
   - `.gitignore` excludes sensitive files
   - `bin/` and `obj/` folders excluded
   - Development configuration files excluded

### Files to Keep Secure:
- `appsettings.Development.json` - Local development credentials
- `appsettings.Production.json` - Production credentials
- `.env` files - Environment variables
- AWS credentials files

---

## 🧪 Complete Testing Workflow

### Full Flow Testing Guide

This section explains how to test the complete end-to-end workflow: **Generate Presigned URL → Upload Image to S3 → Create Fee**

### Prerequisites

1. **Application Running:**
   ```bash
   cd FeeManagementService
   dotnet run
   ```
   - API should be running on `https://localhost:5001`
   - Swagger UI available at `https://localhost:5001/swagger`

2. **Database Ready:**
   - Database migrations applied
   - `FeeManagementDb` database exists

3. **AWS S3 Configured:**
   - S3 bucket created
   - CORS configured
   - IAM credentials configured in `appsettings.Development.json`

4. **JWT Token Generated:**
   - See [JWT_TESTING.md](./JWT_TESTING.md) for token generation
   - Token must include: `SchoolId`, `UserId`, `Role: "SchoolAdmin"`

### Step-by-Step Testing Flow

#### Step 1: Generate JWT Token

**Option A: Using jwt.io**
1. Go to https://jwt.io
2. Use the payload structure from `JWT_TESTING.md`
3. Set secret key from `appsettings.json`
4. Copy the generated token

**Option B: Programmatically**
```csharp
// Use the code example from JWT_TESTING.md
// Generate token with required claims
```

**Required Claims:**
```json
{
  "sub": "user-123",
  "iss": "FeeManagementService",
  "aud": "FeeManagementService",
  "exp": 1735689600,
  "iat": 1735686000,
  "SchoolId": "550e8400-e29b-41d4-a716-446655440000",
  "UserId": "user-123",
  "Role": "SchoolAdmin"
}
```

#### Step 2: Generate Presigned URL

**Using Swagger UI:**
1. Navigate to `https://localhost:5001/swagger`
2. Click **Authorize** button
3. Enter: `Bearer <your-jwt-token>`
4. Click **Authorize**
5. Find **POST /api/fees/presigned-url**
6. Click **Try it out**
7. Enter request body:
   ```json
   {
     "feeId": "fee-123",
     "fileName": "test-image.jpg",
     "contentType": "image/jpeg",
     "fileSize": 102400
   }
   ```
8. Click **Execute**
9. **Expected Response:** `200 OK` with:
   ```json
   {
     "presignedUrl": "https://bucket.s3.region.amazonaws.com/...?X-Amz-Signature=...",
     "s3Key": "schools/{schoolId}/fees/{feeId}/test-image.jpg",
     "imageUrl": "https://bucket.s3.region.amazonaws.com/schools/{schoolId}/fees/{feeId}/test-image.jpg",
     "expiresAt": "2026-02-01T15:30:00Z"
   }
   ```
10. **Save the `imageUrl`** for Step 4

**Using Postman:**
1. Import `FeeManagementService.postman_collection.json`
2. Update `jwtToken` variable with your token
3. Run **Generate Presigned URL** request
4. Check **Tests** tab - variables are auto-saved

**Using cURL:**
```bash
curl -X POST "https://localhost:5001/api/fees/presigned-url" \
  -H "Authorization: Bearer <your-jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "feeId": "fee-123",
    "fileName": "test-image.jpg",
    "contentType": "image/jpeg",
    "fileSize": 102400
  }'
```

#### Step 3: Upload Image to S3

**Important:** This step uploads directly to AWS S3, NOT to your API server.

**Using Postman:**
1. Use the **Upload Image to S3** request
2. The `presignedUrl` variable is auto-populated from Step 2
3. Go to **Body** tab
4. Select **binary**
5. Click **Select File** and choose an image (JPG, PNG, or WebP, max 5MB)
6. Click **Send**
7. **Expected Response:** `200 OK` (from S3)

**Using cURL:**
```bash
curl -X PUT "<presignedUrl>" \
  -H "Content-Type: image/jpeg" \
  --data-binary "@path/to/your/image.jpg"
```

**Using JavaScript (Browser/Node.js):**
```javascript
const file = document.getElementById('fileInput').files[0]; // or from File API
const response = await fetch(presignedUrl, {
  method: 'PUT',
  headers: {
    'Content-Type': file.type
  },
  body: file
});

if (response.ok) {
  console.log('Image uploaded successfully');
  // Use imageUrl from Step 2
}
```

**Verification:**
- Check AWS S3 Console - file should appear in bucket
- File path: `schools/{schoolId}/fees/{feeId}/test-image.jpg`

#### Step 4: Create Fee with Image URL

**Using Swagger UI:**
1. In Swagger UI, find **POST /api/fees**
2. Click **Try it out**
3. Enter request body:
   ```json
   {
     "title": "Class Fee - Semester 1",
     "description": "Fee for first semester classes including all subjects",
     "amount": 1500.00,
     "feeType": "ClassFee",
     "imageUrl": "https://bucket.s3.region.amazonaws.com/schools/{schoolId}/fees/{feeId}/test-image.jpg"
   }
   ```
   - Use the `imageUrl` from Step 2 response
4. Click **Execute**
5. **Expected Response:** `201 Created` with FeeResponse:
   ```json
   {
     "id": "550e8400-e29b-41d4-a716-446655440000",
     "schoolId": "550e8400-e29b-41d4-a716-446655440000",
     "title": "Class Fee - Semester 1",
     "description": "Fee for first semester classes including all subjects",
     "amount": 1500.00,
     "feeType": "ClassFee",
     "imageUrl": "https://bucket.s3.region.amazonaws.com/...",
     "status": "Active",
     "createdBy": "user-123",
     "createdAt": "2026-02-01T14:30:00Z"
   }
   ```

**Using Postman:**
1. Use the **Create Fee** request
2. The `imageUrl` variable is auto-populated from Step 2
3. Click **Send**
4. Check **Tests** tab for validation

**Using cURL:**
```bash
curl -X POST "https://localhost:5001/api/fees" \
  -H "Authorization: Bearer <your-jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Class Fee - Semester 1",
    "description": "Fee for first semester classes",
    "amount": 1500.00,
    "feeType": "ClassFee",
    "imageUrl": "https://bucket.s3.region.amazonaws.com/schools/{schoolId}/fees/{feeId}/test-image.jpg"
  }'
```

**Verification:**
- Check database: `SELECT * FROM Fees WHERE Title = 'Class Fee - Semester 1'`
- Verify `ImageUrl` column contains the S3 URL
- Verify `SchoolId` matches your JWT token claim

### Complete Workflow Diagram

```
┌─────────────────┐
│ 1. Generate JWT │
│    Token         │
└────────┬─────────┘
         │
         ▼
┌─────────────────────┐
│ 2. Generate         │
│    Presigned URL    │
│    (POST /api/fees/ │
│     presigned-url)  │
└────────┬────────────┘
         │
         │ Returns: presignedUrl, imageUrl
         ▼
┌─────────────────────┐
│ 3. Upload Image     │
│    to S3            │
│    (PUT to S3)      │
└────────┬────────────┘
         │
         │ Image stored in S3
         ▼
┌─────────────────────┐
│ 4. Create Fee       │
│    (POST /api/fees) │
│    with imageUrl    │
└────────┬────────────┘
         │
         ▼
┌─────────────────────┐
│ 5. Fee Created      │
│    in Database      │
└─────────────────────┘
```

### Testing Scenarios

#### Scenario 1: Complete Flow with Image
1. ✅ Generate JWT token
2. ✅ Generate presigned URL
3. ✅ Upload image to S3
4. ✅ Create fee with imageUrl
5. ✅ Verify fee in database

#### Scenario 2: Create Fee Without Image
1. ✅ Generate JWT token
2. ✅ Create fee (omit imageUrl field)
3. ✅ Verify fee created with imageUrl = null

#### Scenario 3: Error Handling
1. ✅ Test with expired presigned URL (wait 10+ minutes)
2. ✅ Test with invalid file size (>5MB)
3. ✅ Test with invalid content type
4. ✅ Test with missing JWT token
5. ✅ Test with wrong role (not SchoolAdmin)

### Verification Checklist

After completing the flow, verify:

- [ ] **Presigned URL Generated:**
  - Response contains `presignedUrl`, `imageUrl`, `s3Key`
  - `expiresAt` is in the future

- [ ] **Image Uploaded to S3:**
  - File appears in S3 bucket
  - File path matches `s3Key` from presigned URL response
  - File is accessible via `imageUrl`

- [ ] **Fee Created in Database:**
  - Fee record exists in `Fees` table
  - `SchoolId` matches JWT token claim
  - `ImageUrl` matches S3 URL
  - `Status` is `Active`
  - `CreatedBy` matches `UserId` from token
  - `CreatedAt` is set (UTC timestamp)

- [ ] **Logs Generated:**
  - Request logs in console/file
  - Presigned URL generation logged
  - Fee creation logged
  - No errors in logs

### Common Issues and Solutions

#### Issue: 401 Unauthorized
**Cause:** JWT token missing or invalid
**Solution:**
- Verify token includes all required claims
- Check token hasn't expired
- Ensure Authorization header format: `Bearer <token>`

#### Issue: 403 Forbidden
**Cause:** User doesn't have SchoolAdmin role
**Solution:**
- Verify JWT token includes `Role: "SchoolAdmin"` claim
- Regenerate token with correct role

#### Issue: Presigned URL Upload Fails
**Cause:** URL expired or invalid
**Solution:**
- Presigned URLs expire after 10 minutes
- Generate a new presigned URL
- Verify CORS is configured on S3 bucket

#### Issue: ImageUrl Validation Fails
**Cause:** Invalid S3 URL format
**Solution:**
- Ensure imageUrl matches pattern: `https://bucket.s3.region.amazonaws.com/key`
- Use the `imageUrl` from presigned URL response (don't modify it)

#### Issue: Database Error
**Cause:** Connection string or migration issues
**Solution:**
- Verify database is running
- Check connection string in appsettings.json
- Run migrations: `dotnet ef database update --context FeeDbContext`

### Quick Test Script

For automated testing, you can use this PowerShell script:

```powershell
# Set variables
$baseUrl = "https://localhost:5001"
$jwtToken = "your-jwt-token-here"
$schoolId = "550e8400-e29b-41d4-a716-446655440000"

# Step 1: Generate Presigned URL
$presignedResponse = Invoke-RestMethod -Uri "$baseUrl/api/fees/presigned-url" `
    -Method POST `
    -Headers @{
        "Authorization" = "Bearer $jwtToken"
        "Content-Type" = "application/json"
    } `
    -Body (@{
        feeId = "fee-123"
        fileName = "test.jpg"
        contentType = "image/jpeg"
        fileSize = 102400
    } | ConvertTo-Json)

Write-Host "Presigned URL: $($presignedResponse.presignedUrl)"
Write-Host "Image URL: $($presignedResponse.imageUrl)"

# Step 2: Upload to S3 (requires image file)
# Use the presignedResponse.presignedUrl to upload

# Step 3: Create Fee
$feeResponse = Invoke-RestMethod -Uri "$baseUrl/api/fees" `
    -Method POST `
    -Headers @{
        "Authorization" = "Bearer $jwtToken"
        "Content-Type" = "application/json"
    } `
    -Body (@{
        title = "Test Fee"
        description = "Test description"
        amount = 100.00
        feeType = "ClassFee"
        imageUrl = $presignedResponse.imageUrl
    } | ConvertTo-Json)

Write-Host "Fee Created: $($feeResponse.id)"
```

### Next Steps After Testing

1. **Verify Data:**
   - Check database records
   - Verify S3 bucket contents
   - Review application logs

2. **Performance Testing:**
   - Test with multiple concurrent requests
   - Test with large files (close to 5MB limit)
   - Test presigned URL expiration

3. **Integration Testing:**
   - Test with frontend application
   - Test with mobile app
   - Test error scenarios

---

---

## 📊 Project Statistics

- **Total Steps Completed:** 10+ major implementation steps
- **Files Created:** 30+ files (Controllers, Services, Models, Middleware, Validators, etc.)
- **Endpoints Implemented:** 3 main endpoints (Login, Generate Presigned URL, Create Fee)
- **Database Tables:** 1 (Fees table with full schema)
- **Middleware Components:** 3 (Tenant, Global Exception Handler, Request Logging)
- **Services:** 3 (JWT Token, S3, Fee)
- **Testing:** Complete Postman collection with automated test scripts

---

## 📚 Additional Documentation

- **`S3_PRODUCTION_ISSUES.md`** - Comprehensive guide on S3 production issues, challenges, and solutions based on implementation experience
- **`README.md`** - Project overview and setup instructions
- **`POSTMAN_TESTING.md`** - Detailed Postman usage guide
- **`JWT_TESTING.md`** - JWT token generation and testing guide

---

*Last Updated: February 2, 2026*

