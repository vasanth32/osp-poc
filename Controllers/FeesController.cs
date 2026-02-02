using FeeManagementService.Middleware;
using FeeManagementService.Models;
using FeeManagementService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeeManagementService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FeesController : ControllerBase
{
    private readonly IFeeService _feeService;
    private readonly IS3Service _s3Service;
    private readonly ILogger<FeesController> _logger;
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
    private static readonly string[] AllowedContentTypes = 
    { 
        "image/jpeg", 
        "image/jpg", 
        "image/png", 
        "image/webp" 
    };

    public FeesController(
        IFeeService feeService,
        IS3Service s3Service,
        ILogger<FeesController> logger)
    {
        _feeService = feeService;
        _s3Service = s3Service;
        _logger = logger;
    }

    /// <summary>
    /// Generates a presigned URL for direct image upload to S3
    /// </summary>
    /// <param name="request">Request containing feeId, fileName, contentType, and fileSize</param>
    /// <returns>Presigned URL response with upload URL and image URL</returns>
    /// <response code="200">Presigned URL generated successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">Unauthorized - SchoolId not found or user not authenticated</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("presigned-url")]
    [Authorize(Roles = "SchoolAdmin")]
    [ProducesResponseType(typeof(PresignedUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GeneratePresignedUrl([FromBody] GeneratePresignedUrlRequest request)
    {
        try
        {
            _logger.LogInformation(
                "GeneratePresignedUrl request: FeeId={FeeId}, FileName={FileName}, ContentType={ContentType}, FileSize={FileSize}",
                request.FeeId, request.FileName, request.ContentType, request.FileSize);

            // Get SchoolId from HttpContext (set by TenantMiddleware)
            var schoolId = HttpContext.GetSchoolId();
            if (string.IsNullOrWhiteSpace(schoolId))
            {
                _logger.LogWarning("SchoolId not found in HttpContext for presigned URL generation");
                return Problem(
                    title: "Unauthorized",
                    detail: "SchoolId not found in token. Please ensure you are authenticated with a valid token.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // Validate file size
            if (request.FileSize <= 0 || request.FileSize > MaxFileSize)
            {
                _logger.LogWarning("Invalid file size: {FileSize} bytes", request.FileSize);
                return Problem(
                    title: "Invalid File Size",
                    detail: $"File size must be between 1 byte and {MaxFileSize} bytes (5MB).",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            // Validate content type
            if (string.IsNullOrWhiteSpace(request.ContentType) ||
                !AllowedContentTypes.Contains(request.ContentType.ToLowerInvariant()))
            {
                _logger.LogWarning("Invalid content type: {ContentType}", request.ContentType);
                return Problem(
                    title: "Invalid Content Type",
                    detail: $"Content type must be one of: {string.Join(", ", AllowedContentTypes)}",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            // Generate presigned URL
            var response = await _s3Service.GeneratePresignedUrlAsync(
                schoolId: schoolId,
                feeId: request.FeeId,
                fileName: request.FileName,
                contentType: request.ContentType,
                fileSize: request.FileSize);

            _logger.LogInformation(
                "Presigned URL generated successfully: FeeId={FeeId}, S3Key={S3Key}",
                request.FeeId, response.S3Key);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in GeneratePresignedUrl: {Message}", ex.Message);
            return Problem(
                title: "Invalid Request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access in GeneratePresignedUrl: {Message}", ex.Message);
            return Problem(
                title: "Unauthorized",
                detail: ex.Message,
                statusCode: StatusCodes.Status401Unauthorized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GeneratePresignedUrl");
            return Problem(
                title: "Internal Server Error",
                detail: "An error occurred while generating the presigned URL. Please try again later.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Creates a new fee for the authenticated school
    /// </summary>
    /// <param name="request">Fee creation request with title, description, amount, feeType, and imageUrl</param>
    /// <returns>Created fee response</returns>
    /// <response code="201">Fee created successfully</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="401">Unauthorized - SchoolId or UserId not found</response>
    /// <response code="403">Forbidden - Operation not allowed</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [Authorize(Roles = "SchoolAdmin")]
    [ProducesResponseType(typeof(FeeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateFee([FromBody] CreateFeeRequest request)
    {
        try
        {
            _logger.LogInformation(
                "CreateFee request: Title={Title}, Amount={Amount}, FeeType={FeeType}",
                request.Title, request.Amount, request.FeeType);

            // Get SchoolId from HttpContext (set by TenantMiddleware)
            var schoolId = HttpContext.GetSchoolId();
            if (string.IsNullOrWhiteSpace(schoolId))
            {
                _logger.LogWarning("SchoolId not found in HttpContext for fee creation");
                return Problem(
                    title: "Unauthorized",
                    detail: "SchoolId not found in token. Please ensure you are authenticated with a valid token.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // Get UserId from HttpContext (set by TenantMiddleware) or fallback to User.Identity.Name
            var userId = HttpContext.GetUserId() ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("UserId not found in HttpContext or User.Identity for fee creation");
                return Problem(
                    title: "Unauthorized",
                    detail: "UserId not found. Please ensure you are authenticated with a valid token.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // Create fee using service
            var feeResponse = await _feeService.CreateFeeAsync(request, schoolId, userId);

            _logger.LogInformation(
                "Fee created successfully: FeeId={FeeId}, SchoolId={SchoolId}, Title={Title}",
                feeResponse.Id, schoolId, feeResponse.Title);

            // Return 201 Created with the fee response
            // Using Created() instead of CreatedAtAction since GetFeeById is not yet fully implemented
            return StatusCode(StatusCodes.Status201Created, feeResponse);
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error in CreateFee: {Message}", ex.Message);
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
            
            return ValidationProblem(new ValidationProblemDetails(errors)
            {
                Title = "Validation Error",
                Detail = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in CreateFee: {Message}", ex.Message);
            return Problem(
                title: "Invalid Request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access in CreateFee: {Message}", ex.Message);
            return Problem(
                title: "Unauthorized",
                detail: ex.Message,
                statusCode: StatusCodes.Status401Unauthorized);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation in CreateFee: {Message}", ex.Message);
            return Problem(
                title: "Forbidden",
                detail: ex.Message,
                statusCode: StatusCodes.Status403Forbidden);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in CreateFee");
            return Problem(
                title: "Internal Server Error",
                detail: "An error occurred while creating the fee. Please try again later.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Gets a fee by ID (placeholder for CreatedAtAction)
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    private IActionResult GetFeeById(Guid id)
    {
        // This is a placeholder method for CreatedAtAction
        // Will be implemented later
        return NotFound();
    }
}

