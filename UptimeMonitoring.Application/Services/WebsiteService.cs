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
        // Validate URL format
        if (string.IsNullOrWhiteSpace(url))
        {
            return Result.Failure(Error.Validation("URL is required"));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || 
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Result.Failure(Error.Validation("URL must be a valid HTTP or HTTPS URL"));
        }

        // Validate interval range
        if (intervalMinutes < 1 || intervalMinutes > 1440)
        {
            return Result.Failure(Error.Validation("Check interval must be between 1 and 1440 minutes (24 hours)"));
        }

        // Check for duplicate URL for this user
        var existingWebsites = await _repository.GetByUserIdAsync(userId);
        if (existingWebsites.Any(w => w.Url.Equals(url, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(Error.Conflict("A website with this URL already exists"));
        }

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

    public async Task<Result<Website>> PauseAsync(Guid userId, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Result<Website>.Failure(Error.Validation("URL is required"));
        }

        var website = await _repository.GetByUserIdAndUrlAsync(userId, url);

        if (website == null)
            return Result<Website>.Failure(Error.NotFound("Website not found."));

        website.IsActive = false;
        await _repository.UpdateAsync(website);

        return Result<Website>.Success(website);
    }

    public async Task<Result<Website>> ResumeAsync(Guid userId, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Result<Website>.Failure(Error.Validation("URL is required"));
        }

        var website = await _repository.GetByUserIdAndUrlAsync(userId, url);

        if (website == null)
            return Result<Website>.Failure(Error.NotFound("Website not found."));

        website.IsActive = true;
        await _repository.UpdateAsync(website);

        return Result<Website>.Success(website);
    }
}
