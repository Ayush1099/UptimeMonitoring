using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UptimeMonitoring.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [Authorize]
    [HttpGet("secure")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Secure()
    {
        return Ok("You are authenticated 🎉");
    }
}
