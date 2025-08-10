using Microsoft.AspNetCore.Mvc;

namespace EduShield.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            service = "EduShield SIS API"
        });
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("pong");
    }
}
