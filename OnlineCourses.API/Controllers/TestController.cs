using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OnlineCourses.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;
    
    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }
    
    // GET: api/test
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        _logger.LogInformation("Test endpoint called - GET /api/test");
        
        return Ok(new { 
            message = "API is working!",
            timestamp = DateTime.UtcNow,
            status = "online"
        });
    }

    // GET: api/test/health
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
    }
    
    // GET: api/test/authorized
    [HttpGet("authorized")]
    [Authorize]
    public IActionResult GetAuthorized()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        
        _logger.LogInformation("Authorized test endpoint called by UserId: {UserId}, Email: {Email}, Role: {Role}", 
            userId, userEmail, userRole);
        
        return Ok(new { 
            message = "You are authorized!",
            userId = userId,
            email = userEmail,
            role = userRole,
            timestamp = DateTime.UtcNow
        });
    }
    
    // GET: api/test/error
    [HttpGet("error")]
    [AllowAnonymous]
    public IActionResult GetError()
    {
        _logger.LogWarning("Test error endpoint called - returning 500 error");
        
        return StatusCode(500, new { 
            message = "This is a test error",
            timestamp = DateTime.UtcNow
        });
    }
    
    // GET: api/test/notfound
    [HttpGet("notfound")]
    [AllowAnonymous]
    public IActionResult GetNotFound()
    {
        _logger.LogInformation("Test not found endpoint called - returning 404");
        
        return NotFound(new { 
            message = "Resource not found (test)",
            timestamp = DateTime.UtcNow
        });
    }
    
    // POST: api/test/echo
    [HttpPost("echo")]
    [AllowAnonymous]
    public IActionResult Echo([FromBody] object data)
    {
        _logger.LogInformation("Test echo endpoint called with data: {@Data}", data);
        
        return Ok(new { 
            message = "Echo successful",
            receivedData = data,
            timestamp = DateTime.UtcNow
        });
    }
    
    // GET: api/test/throw
    [HttpGet("throw")]
    [AllowAnonymous]
    public IActionResult ThrowException()
    {
        _logger.LogError("Test exception endpoint called - throwing exception");
        
        throw new Exception("This is a test exception from the TestController");
    }
}
