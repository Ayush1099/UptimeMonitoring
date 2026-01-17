using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Domain.Entities;

namespace UptimeMonitoring.Application.Services;

public class WebsiteService
{
    private readonly IWebsiteRepository _repository;
    private readonly IAlertStateStore _alertStateStore;
    public WebsiteService(IWebsiteRepository repository,
    IAlertStateStore alertStateStore)
    {
        _repository = repository; 
        _alertStateStore = alertStateStore;
    }

    public async Task AddWebsiteAsync(Guid userId, string url, int intervalMinutes)
    {
        var website = new Website
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Url = url,
            IsActive = true,
            CheckIntervalMinutes = intervalMinutes,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(website);
    }

    public async Task<List<Website>> GetUserWebsitesAsync(Guid userId)
    {
        return await _repository.GetByUserIdAsync(userId);
    }
    public async Task DeleteWebsiteAsync(Guid userId, Guid websiteId)
    {
        var website = await _repository.GetByIdAsync(websiteId);

        if (website == null)
            throw new Exception("Website not found");

        if (website.UserId != userId)
            throw new UnauthorizedAccessException();

        // Delete Redis alert state
        await _alertStateStore.DeleteStateAsync(websiteId);

        // Delete website from DB
        await _repository.DeleteAsync(website);
    }

    public async Task<Website> PauseAsync(Guid userId, Guid websiteId)
    {
        var website = await _repository.GetByIdAsync(websiteId);

        if (website == null)
            throw new Exception("Website not found");

        if (website.UserId != userId)
            throw new UnauthorizedAccessException();

        website.IsActive = false;
        await _repository.UpdateAsync(website);

        return website;
    }

    public async Task<Website> ResumeAsync(Guid userId, Guid websiteId)
    {
        var website = await _repository.GetByIdAsync(websiteId);

        if (website == null)
            throw new Exception("Website not found");

        if (website.UserId != userId)
            throw new UnauthorizedAccessException();

        website.IsActive = true;
        await _repository.UpdateAsync(website);

        return website;
    }
}
