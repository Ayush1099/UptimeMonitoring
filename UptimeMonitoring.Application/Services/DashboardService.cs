using UptimeMonitoring.Application.DTOs;
using UptimeMonitoring.Application.Interfaces;

namespace UptimeMonitoring.Application.Services;

public class DashboardService
{
    private readonly IWebsiteRepository _websiteRepository;
    private readonly IMonitoringResultRepository _resultRepository;

    public DashboardService(IWebsiteRepository websiteRepository,IMonitoringResultRepository resultRepository)
    {
        _websiteRepository = websiteRepository;
        _resultRepository = resultRepository;
    }

    public async Task<List<DashboardWebsiteStatusResponse>> GetStatusAsync(Guid userId)
    {
        var websites = await _websiteRepository.GetByUserIdAsync(userId);
        var fromUtc = DateTime.UtcNow.AddHours(-24);
        var result = new List<DashboardWebsiteStatusResponse>();
        foreach (var website in websites)
        {
            if (!website.IsActive)
            {
                result.Add(new DashboardWebsiteStatusResponse
                {
                    WebsiteId = website.Id,
                    Url = website.Url,
                    Status = "PAUSED",
                    UptimePercentage = null
                });
                continue;
            }
            var latest = await _resultRepository.GetLatestByWebsiteIdAsync(website.Id);
            var (total, up) =await _resultRepository.GetStatsAsync(website.Id,fromUtc);
            double? uptime = total == 0 ? null : Math.Round((double)up / total * 100, 2);
            result.Add(new DashboardWebsiteStatusResponse
            {
                WebsiteId = website.Id,
                Url = website.Url,
                Status = latest == null
                    ? "UNKNOWN"
                    : latest.IsUp ? "UP" : "DOWN",
                LastCheckedAt = latest?.CheckedAt,
                ResponseTimeMs = latest?.ResponseTimeMs,
                UptimePercentage = uptime
            });
        }
        return result;
    }

}
