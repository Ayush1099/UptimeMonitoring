using System.Diagnostics;
using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Domain.Entities;

namespace UptimeMonitoring.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HttpClient _httpClient = new();

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            using var scope = _scopeFactory.CreateScope();

            var websiteRepository =
                scope.ServiceProvider.GetRequiredService<IWebsiteRepository>();

            var resultRepository =
                scope.ServiceProvider.GetRequiredService<IMonitoringResultRepository>();

            var userRepository =
                scope.ServiceProvider.GetRequiredService<IUserRepository>();

            var alertStateStore =
                scope.ServiceProvider.GetRequiredService<IAlertStateStore>();

            var emailSender =
                scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var websites = await websiteRepository.GetAllActiveAsync();

            foreach (var website in websites)
            {
                try
                {
                    var latest = await resultRepository.GetLatestByWebsiteIdAsync(website.Id);
                    var intervalMinutes = website.CheckIntervalMinutes <= 0 ? 5 : website.CheckIntervalMinutes;
                    var interval = TimeSpan.FromMinutes(intervalMinutes);

                    if (latest != null && (DateTime.UtcNow - latest.CheckedAt) < interval)
                        continue;

                    await CheckWebsiteAsync(
                        website,
                        resultRepository,
                        alertStateStore,
                        userRepository,
                        emailSender,
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking website {WebsiteId} ({Url})", website.Id, website.Url);
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task CheckWebsiteAsync(
        Website website,
        IMonitoringResultRepository resultRepository,
        IAlertStateStore alertStateStore,
        IUserRepository userRepository,
        IEmailSender emailSender,
        CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();
        bool isUp;

        try
        {
            var response = await _httpClient.GetAsync(website.Url, stoppingToken);
            isUp = response.IsSuccessStatusCode;
        }
        catch
        {
            isUp = false;
        }

        stopwatch.Stop();

        var result = new MonitoringResult
        {
            Id = Guid.NewGuid(),
            WebsiteId = website.Id,
            IsUp = isUp,
            ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
            CheckedAt = DateTime.UtcNow
        };

        await resultRepository.AddAsync(result);

        await HandleAlertAsync(
            website,
            isUp,
            alertStateStore,
            userRepository,
            emailSender,
            stoppingToken);

        _logger.LogInformation(
            "Checked {url} - Status: {status}",
            website.Url,
            isUp ? "UP" : "DOWN"
        );
    }

    private async Task HandleAlertAsync(
        Website website,
        bool isUp,
        IAlertStateStore alertStateStore,
        IUserRepository userRepository,
        IEmailSender emailSender,
        CancellationToken stoppingToken)
    {
        var lastState = await alertStateStore.GetLastStateAsync(website.Id);

        if (lastState == null)
        {
            await alertStateStore.SetStateAsync(website.Id, isUp);
            return;
        }
        if (lastState.Value == isUp)
            return;

        var user = await userRepository.GetByIdAsync(website.UserId);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            await alertStateStore.SetStateAsync(website.Id, isUp);
            return;
        }

        if (lastState.Value && !isUp)
        {
            await emailSender.SendAsync(
                user.Email,
                $"[UptimeMonitoring] DOWN: {website.Url}",
                $"{website.Url} appears DOWN at {DateTime.UtcNow:O}.",
                stoppingToken);
        }
        else if (!lastState.Value && isUp)
        {
            await emailSender.SendAsync(
                user.Email,
                $"[UptimeMonitoring] RECOVERED: {website.Url}",
                $"{website.Url} is back UP at {DateTime.UtcNow:O}.",
                stoppingToken);
        }

        await alertStateStore.SetStateAsync(website.Id, isUp);
    }
}
