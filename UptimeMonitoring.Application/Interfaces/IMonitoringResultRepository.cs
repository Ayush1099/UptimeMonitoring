using UptimeMonitoring.Domain.Entities;

namespace UptimeMonitoring.Application.Interfaces;

public interface IMonitoringResultRepository
{
    Task AddAsync(MonitoringResult result);
    Task<MonitoringResult?> GetLatestByWebsiteIdAsync(Guid websiteId);
    Task<(int total, int up)> GetStatsAsync(Guid websiteId,DateTime fromUtc);

}
