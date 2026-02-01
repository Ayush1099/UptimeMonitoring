using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)] // Added for duplicate website
    public async Task<IActionResult> Add(AddWebsiteRequest request)
    {
        var existingWebsites = await _service.GetUserWebsitesAsync(GetUserId());
        if (existingWebsites.Any(w => w.Url == request.Url))
        {
            return Conflict("Website with this URL already exists for the user.");
        }

        await _service.AddWebsiteAsync(
            GetUserId(),
            request.Url,
            request.CheckIntervalMinutes
        );

        return Ok();
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WebsiteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid websiteId)
    {
        bool deleted = await _service.DeleteWebsiteAsync(
            GetUserId(),
            websiteId
        );

        if (!deleted)
        {
            return NoContent();
        }

        return Ok("Website deleted successfully.");
    }
    [HttpPost("{websiteId:guid}/pause")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Pause(Guid websiteId)
    {
        var website = await _service.PauseAsync(GetUserId(), websiteId);
        if (website == null)
        {
            return NoContent();
        }
        return Ok(new
        {
            website.Id,
            website.Url,
            website.IsActive,
            Status = "PAUSED"
        });
    }
    [HttpPost("{websiteId:guid}/resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Resume(Guid websiteId)
    {
        var website = await _service.ResumeAsync(GetUserId(), websiteId);
        if(website == null)
        {
            return NoContent();
        }

        return Ok(new
        {
            website.Id,
            website.Url,
            website.IsActive,
            Status = "ACTIVE"
        });
    }
}
