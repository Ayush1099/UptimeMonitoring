using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UptimeMonitoring.Application.Services;
using UptimeMonitoring.Domain.Entities;

namespace UptimeMonitoring.Web.Pages.Websites;

[Authorize]
public class IndexModel : PageModel
{
    private readonly WebsiteService _websiteService;

    public IndexModel(WebsiteService websiteService)
    {
        _websiteService = websiteService;
    }

    public List<Website> Websites { get; set; } = [];

    [BindProperty]
    public AddInputModel AddInput { get; set; } = new();

    public class AddInputModel
    {
        [Required(ErrorMessage = "URL is required")]
        [Url(ErrorMessage = "URL must be a valid HTTP or HTTPS URL")]
        public string Url { get; set; } = null!;

        [Range(1, 1440)]
        public int CheckIntervalMinutes { get; set; } = 5;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task OnGetAsync()
    {
        var result = await _websiteService.GetUserWebsitesAsync(GetUserId());
        Websites = result.IsSuccess ? result.Value! : [];
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!ModelState.IsValid)
        {
            var result = await _websiteService.GetUserWebsitesAsync(GetUserId());
            Websites = result.IsSuccess ? result.Value! : [];
            return Page();
        }

        var addResult = await _websiteService.AddWebsiteAsync(
            GetUserId(),
            AddInput.Url.Trim(),
            AddInput.CheckIntervalMinutes);

        if (addResult.IsFailure)
        {
            TempData["Error"] = addResult.Error!.Message;
            return RedirectToPage();
        }

        TempData["Success"] = "Website added successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPauseAsync(string url)
    {
        var result = await _websiteService.PauseAsync(GetUserId(), url);
        if (result.IsFailure)
            TempData["Error"] = result.Error!.Message;
        else
            TempData["Success"] = "Website paused.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResumeAsync(string url)
    {
        var result = await _websiteService.ResumeAsync(GetUserId(), url);
        if (result.IsFailure)
            TempData["Error"] = result.Error!.Message;
        else
            TempData["Success"] = "Website resumed.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid websiteId)
    {
        var result = await _websiteService.DeleteWebsiteAsync(GetUserId(), websiteId);
        if (result.IsFailure)
            TempData["Error"] = result.Error!.Message;
        else
            TempData["Success"] = "Website deleted.";
        return RedirectToPage();
    }
}
