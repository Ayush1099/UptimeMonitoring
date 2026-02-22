using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UptimeMonitoring.Application.DTOs;
using UptimeMonitoring.Application.Services;

namespace UptimeMonitoring.Web.Pages;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly DashboardService _dashboardService;

    public DashboardModel(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public List<DashboardWebsiteStatusResponse> Statuses { get; set; } = [];

    public async Task OnGetAsync()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Statuses = await _dashboardService.GetStatusAsync(userId);
    }
}
