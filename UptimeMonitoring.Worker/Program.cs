using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Infrastructure.Email;
using UptimeMonitoring.Infrastructure.Persistence;
using UptimeMonitoring.Infrastructure.Redis;
using UptimeMonitoring.Infrastructure.Repositories;
using UptimeMonitoring.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
    
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<HostOptions>(opts =>
    opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

builder.Services.AddScoped<IWebsiteRepository, WebsiteRepository>();
builder.Services.AddScoped<IMonitoringResultRepository, MonitoringResultRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var redisConnection = configuration.GetValue<string>("Redis:Connection")
                         ?? "localhost:6379";

    var options = ConfigurationOptions.Parse(redisConnection);
    options.AbortOnConnectFail = false;
    options.ConnectRetry = 5;
    options.ReconnectRetryPolicy = new ExponentialRetry(5000);
    return ConnectionMultiplexer.Connect(options);
});

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IAlertStateStore, AlertStateStore>();


var host = builder.Build();

// Ensure database is created/migrated (dev-friendly default)
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

host.Run();
