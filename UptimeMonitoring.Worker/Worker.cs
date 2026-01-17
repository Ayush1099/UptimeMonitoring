using System.Diagnostics;
using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Domain.Entities;
using UptimeMonitoring.Infrastructure.Email;
using UptimeMonitoring.Infrastructure.Redis;

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

            var websites = await websiteRepository.GetAllActiveAsync();

            var services = scope.ServiceProvider;

            foreach (var website in websites)
            {
                await CheckWebsiteAsync(website, services);
            }
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }

    private async Task CheckWebsiteAsync(
        Website website,
        IServiceProvider services)
    {
        var alertStore = services.GetRequiredService<AlertStateStore>();
        var emailSender = services.GetRequiredService<SmtpEmailSender>();
        var resultRepo = services.GetRequiredService<IMonitoringResultRepository>();

        var stopwatch = Stopwatch.StartNew();
        bool isUp;

        try
        {
            var response = await _httpClient.GetAsync(website.Url);
            isUp = response.IsSuccessStatusCode;
        }
        catch
        {
            isUp = false;
        }

        stopwatch.Stop();

        // Save result
        await resultRepo.AddAsync(new MonitoringResult
        {
            Id = Guid.NewGuid(),
            WebsiteId = website.Id,
            IsUp = isUp,
            ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
            CheckedAt = DateTime.UtcNow
        });

        // Redis logic
        var lastState = await alertStore.GetLastStateAsync(website.Id);

        if (lastState == null)
        {
            // first time
            await alertStore.SetStateAsync(website.Id, isUp);
            return;
        }

        // DOWN detected
        if (lastState == true && isUp == false)
        {
            await emailSender.SendAsync(
                "ayush99verma@email.com",
                "Website DOWN",
                $"{website.Url} is DOWN"
            );
        }

        // UP recovery
        if (lastState == false && isUp == true)
        {
            await emailSender.SendAsync(
                "user@email.com",
                "Website UP",
                $"{website.Url} is back UP"
            );
        }

        await alertStore.SetStateAsync(website.Id, isUp);
    }

}
