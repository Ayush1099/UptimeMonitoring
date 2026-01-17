using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UptimeMonitoring.Application.Services;

namespace UptimeMonitoring.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _service;

    public DashboardController(DashboardService service)
    {
        _service = service;
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirstValue("sub")
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.Parse(userId!);
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var result = await _service.GetStatusAsync(GetUserId());
        return Ok(result);
    }
}
