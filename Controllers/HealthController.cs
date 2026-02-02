using Microsoft.AspNetCore.Mvc;

namespace FeeManagementService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint (public, no authentication required)
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", service = "FeeManagementService", version = "1.0.0" });
    }
}

