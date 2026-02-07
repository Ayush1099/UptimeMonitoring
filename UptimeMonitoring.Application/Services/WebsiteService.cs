using UptimeMonitoring.Application.Common;
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

    public async Task<Result> AddWebsiteAsync(Guid userId, string url, int intervalMinutes)
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
        return Result.Success();
    }

    public async Task<Result<List<Website>>> GetUserWebsitesAsync(Guid userId)
    {
        var websites = await _repository.GetByUserIdAsync(userId);
        return Result<List<Website>>.Success(websites);
    }
    public async Task<Result> DeleteWebsiteAsync(Guid userId, Guid websiteId)
    {
        var website = await _repository.GetByIdAsync(websiteId);

        if (website == null)
            return Result.Failure(Error.NotFound("Website not found."));

        if (website.UserId != userId)
            return Result.Failure(Error.Unauthorized("You are not authorized to delete this website."));

        await _alertStateStore.DeleteStateAsync(websiteId);

        await _repository.DeleteAsync(website);

        return Result.Success();
    }

    public async Task<Result<Website>> PauseAsync(Guid userId, Guid websiteId)
    {
        var website = await _repository.GetByIdAsync(websiteId);

        if (website == null)
            return Result<Website>.Failure(Error.NotFound("Website not found."));

        if (website.UserId != userId)
            return Result<Website>.Failure(Error.Unauthorized("You are not authorized to pause this website."));

        website.IsActive = false;
        await _repository.UpdateAsync(website);

        return Result<Website>.Success(website);
    }

    public async Task<Result<Website>> ResumeAsync(Guid userId, Guid websiteId)
    {
        var website = await _repository.GetByIdAsync(websiteId);

        if (website == null)
            return Result<Website>.Failure(Error.NotFound("Website not found."));

        if (website.UserId != userId)
            return Result<Website>.Failure(Error.Unauthorized("You are not authorized to resume this website."));

        website.IsActive = true;
        await _repository.UpdateAsync(website);

        return Result<Website>.Success(website);
    }
}
