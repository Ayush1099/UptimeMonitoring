using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UptimeMonitoring.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "UP",
            service = "UptimeMonitoring API",
            time = DateTime.UtcNow
        });
    }
}
