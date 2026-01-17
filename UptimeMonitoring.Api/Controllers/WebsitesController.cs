using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UptimeMonitoring.Application.DTOs;
using UptimeMonitoring.Application.Services;

namespace UptimeMonitoring.Api.Controllers;

[ApiController]
[Route("api/websites")]
[Authorize]
public class WebsitesController : ControllerBase
{
    private readonly WebsiteService _service;

    public WebsitesController(WebsiteService service)
    {
        _service = service;
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        return Guid.Parse(userId!);
    }

    [HttpPost]
    public async Task<IActionResult> Add(AddWebsiteRequest request)
    {
        await _service.AddWebsiteAsync(
            GetUserId(),
            request.Url,
            request.CheckIntervalMinutes
        );

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var websites = await _service.GetUserWebsitesAsync(GetUserId());

        var result = websites.Select(w => new WebsiteResponse
        {
            Id = w.Id,
            Url = w.Url,
            IsActive = w.IsActive,
            CheckIntervalMinutes = w.CheckIntervalMinutes
        });

        return Ok(result);
    }
    [HttpDelete("{websiteId:guid}")]
    public async Task<IActionResult> Delete(Guid websiteId)
    {
        await _service.DeleteWebsiteAsync(
            GetUserId(),
            websiteId
        );

        return NoContent(); // 204
    }
    [HttpPost("{websiteId:guid}/pause")]
    public async Task<IActionResult> Pause(Guid websiteId)
    {
        var website = await _service.PauseAsync(GetUserId(), websiteId);

        return Ok(new
        {
            website.Id,
            website.Url,
            website.IsActive,
            Status = "PAUSED"
        });
    }
    [HttpPost("{websiteId:guid}/resume")]
    public async Task<IActionResult> Resume(Guid websiteId)
    {
        var website = await _service.ResumeAsync(GetUserId(), websiteId);

        return Ok(new
        {
            website.Id,
            website.Url,
            website.IsActive,
            Status = "ACTIVE"
        });
    }
}
