using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

            // Small tick so per-website intervals work without busy looping
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

        // First observation - record state, no alert
        if (lastState == null)
        {
            await alertStateStore.SetStateAsync(website.Id, isUp);
            return;
        }

        // No change
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




//using System.Diagnostics;
//using UptimeMonitoring.Application.Interfaces;
//using UptimeMonitoring.Domain.Entities;
//using UptimeMonitoring.Infrastructure.Email;
//using UptimeMonitoring.Infrastructure.Redis;

//namespace UptimeMonitoring.Worker;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    private readonly IServiceScopeFactory _scopeFactory;
//    private readonly HttpClient _httpClient = new();
//    private readonly IWebsiteRepository _repository;
//    public Worker(
//        ILogger<Worker> logger,
//        IServiceScopeFactory scopeFactory,
//        IWebsiteRepository repository)
//    {
//        _logger = logger;
//        _scopeFactory = scopeFactory;
//        _repository = repository;
//    }

//    //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    //{
//    //    while (!stoppingToken.IsCancellationRequested)
//    //    {
//    //        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

//    //        using var scope = _scopeFactory.CreateScope();

//    //        var websiteRepository =
//    //            scope.ServiceProvider.GetRequiredService<IWebsiteRepository>();

//    //        var resultRepository =
//    //            scope.ServiceProvider.GetRequiredService<IMonitoringResultRepository>();

//    //        var websites = await websiteRepository.GetAllActiveAsync();

//    //        var services = scope.ServiceProvider;

//    //        foreach (var website in websites)
//    //        {
//    //            await CheckWebsiteAsync(website, services);
//    //        }
//    //        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
//    //    }
//    //}
//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("Worker started");

//        while (!stoppingToken.IsCancellationRequested)
//        {
//            try
//            {
//                var websites = await _repository.GetAllActiveAsync();

//                using var scope = _scopeFactory.CreateScope();
//                var services = scope.ServiceProvider;

//                foreach (var website in websites)
//                {
//                    await CheckWebsiteAsync(website, services);
//                    _logger.LogInformation( "Checking {Url}", website.Url);
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Worker error, will retry");
//            }

//            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
//        }
//    }
//    private async Task CheckWebsiteAsync(
//        Website website,
//        IServiceProvider services)
//    {
//        var alertStore = services.GetRequiredService<AlertStateStore>();
//        var emailSender = services.GetRequiredService<SmtpEmailSender>();
//        var resultRepo = services.GetRequiredService<IMonitoringResultRepository>();

//        var stopwatch = Stopwatch.StartNew();
//        bool isUp;

//        try
//        {
//            var response = await _httpClient.GetAsync(website.Url);
//            isUp = response.IsSuccessStatusCode;
//        }
//        catch
//        {
//            isUp = false;
//        }

//        stopwatch.Stop();

//        // Save result
//        await resultRepo.AddAsync(new MonitoringResult
//        {
//            Id = Guid.NewGuid(),
//            WebsiteId = website.Id,
//            IsUp = isUp,
//            ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
//            CheckedAt = DateTime.UtcNow
//        });

//        // Redis logic
//        var lastState = await alertStore.GetLastStateAsync(website.Id);

//        if (lastState == null)
//        {
//            // first time
//            await alertStore.SetStateAsync(website.Id, isUp);
//            return;
//        }

//        // DOWN detected
//        if (lastState == true && isUp == false)
//        {
//            await emailSender.SendAsync(
//                "ayush99verma@email.com",
//                "Website DOWN",
//                $"{website.Url} is DOWN"
//            );
//        }

//        // UP recovery
//        if (lastState == false && isUp == true)
//        {
//            await emailSender.SendAsync(
//                "user@email.com",
//                "Website UP",
//                $"{website.Url} is back UP"
//            );
//        }

//        await alertStore.SetStateAsync(website.Id, isUp);
//    }

//}
