using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UptimeMonitoring.Application.Common;
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
        var result = await _service.AddWebsiteAsync(
            GetUserId(),
            request.Url,
            request.CheckIntervalMinutes
        );

        if (result.IsFailure)
        {
            return result.Error!.Code switch
            {
                "Conflict" => Conflict(result.Error.Message),
                _ => BadRequest(result.Error.Message),
            };
        }

        return Ok();
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WebsiteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get()
    {
        var result = await _service.GetUserWebsitesAsync(GetUserId());

        if (result.IsFailure)
        {
            return result.Error!.Code switch
            {
                "Unauthorized" => Unauthorized(result.Error.Message),
                _ => BadRequest(result.Error.Message),
            };
        }

        var websites = result.Value!;

        var response = websites.Select(w => new WebsiteResponse
        {
            Id = w.Id,
            Url = w.Url,
            IsActive = w.IsActive,
            CheckIntervalMinutes = w.CheckIntervalMinutes
        });

        return Ok(response);
    }
    [HttpDelete("{websiteId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid websiteId)
    {
        var result = await _service.DeleteWebsiteAsync(
            GetUserId(),
            websiteId
        );

        if (result.IsFailure)
        {
            return result.Error!.Code switch
            {
                "NotFound" => NotFound(result.Error.Message),
                "Unauthorized" => Unauthorized(result.Error.Message),
                _ => BadRequest(result.Error.Message),
            };
        }

        return Ok("Website deleted successfully.");
    }
    [HttpPost("{websiteId:guid}/pause")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Pause(Guid websiteId)
    {
        var result = await _service.PauseAsync(GetUserId(), websiteId);
        if (result.IsFailure)
        {
            return result.Error!.Code switch
            {
                "NotFound" => NotFound(result.Error.Message),
                "Unauthorized" => Unauthorized(result.Error.Message),
                _ => BadRequest(result.Error.Message),
            };
        }
        var website = result.Value!;
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
        var result = await _service.ResumeAsync(GetUserId(), websiteId);
        if(result.IsFailure)
        {
            return result.Error!.Code switch
            {
                "NotFound" => NotFound(result.Error.Message),
                "Unauthorized" => Unauthorized(result.Error.Message),
                _ => BadRequest(result.Error.Message),
            };
        }

        var website = result.Value!;

        return Ok(new
        {
            website.Id,
            website.Url,
            website.IsActive,
            Status = "ACTIVE"
        });
    }
}
