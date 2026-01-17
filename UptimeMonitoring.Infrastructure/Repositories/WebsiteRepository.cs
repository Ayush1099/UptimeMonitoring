using Microsoft.EntityFrameworkCore;
using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Domain.Entities;
using UptimeMonitoring.Infrastructure.Persistence;

namespace UptimeMonitoring.Infrastructure.Repositories;

public class WebsiteRepository : IWebsiteRepository
{
    private readonly ApplicationDbContext _dbContext;

    public WebsiteRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Website website)
    {
        _dbContext.Websites.Add(website);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<Website>> GetByUserIdAsync(Guid userId)
    {
        return await _dbContext.Websites
            .Where(w => w.UserId == userId)
            .ToListAsync();
    }
    public async Task<List<Website>> GetAllActiveAsync()
    {
        return await _dbContext.Websites
            .Where(w => w.IsActive)
            .ToListAsync();
    }
    public async Task<Website?> GetByIdAsync(Guid websiteId)
    {
        return await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == websiteId);
    }

    public async Task DeleteAsync(Website website)
    {
        _dbContext.Websites.Remove(website);
        await _dbContext.SaveChangesAsync();
    }
    public async Task UpdateAsync(Website website)
    {
        _dbContext.Websites.Update(website);
        await _dbContext.SaveChangesAsync();
    }


}
