using Microsoft.EntityFrameworkCore;
using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Domain.Entities;
using UptimeMonitoring.Infrastructure.Persistence;

namespace UptimeMonitoring.Infrastructure.Repositories;

public class MonitoringResultRepository : IMonitoringResultRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MonitoringResultRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(MonitoringResult result)
    {
        _dbContext.MonitoringResults.Add(result);
        await _dbContext.SaveChangesAsync();
    }
    public async Task<MonitoringResult?> GetLatestByWebsiteIdAsync(Guid websiteId)
    {
        return await _dbContext.MonitoringResults
            .Where(r => r.WebsiteId == websiteId)
            .OrderByDescending(r => r.CheckedAt)
            .FirstOrDefaultAsync();
    }
    public async Task<(int total, int up)> GetStatsAsync(
    Guid websiteId,
    DateTime fromUtc)
    {
        var query = _dbContext.MonitoringResults
            .Where(r =>
                r.WebsiteId == websiteId &&
                r.CheckedAt >= fromUtc
            );

        var total = await query.CountAsync();
        var up = await query.CountAsync(r => r.IsUp);

        return (total, up);
    }
}
