using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Infrastructure.Email;
using UptimeMonitoring.Infrastructure.Persistence;
using UptimeMonitoring.Infrastructure.Redis;
using UptimeMonitoring.Infrastructure.Repositories;
using UptimeMonitoring.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379")
);

builder.Services.AddScoped<AlertStateStore>();
builder.Services.AddScoped<IMonitoringResultRepository, MonitoringResultRepository>();
builder.Services.AddScoped<IWebsiteRepository, WebsiteRepository>();
builder.Services.AddScoped<SmtpEmailSender>();
builder.Services.AddScoped<IAlertStateStore, AlertStateStore>();


var host = builder.Build();
host.Run();
