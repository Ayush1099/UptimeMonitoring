using UptimeMonitoring.Domain.Entities;

namespace UptimeMonitoring.Application.Interfaces;

public interface IWebsiteRepository
{
    Task AddAsync(Website website);
    Task<List<Website>> GetByUserIdAsync(Guid userId);
    Task<List<Website>> GetAllActiveAsync();
    Task<Website?> GetByIdAsync(Guid websiteId);
    Task<Website?> GetByUserIdAndUrlAsync(Guid userId, string url);
    Task DeleteAsync(Website website);
    Task UpdateAsync(Website website);

}
