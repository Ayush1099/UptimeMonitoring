using Microsoft.EntityFrameworkCore;
using UptimeMonitoring.Domain.Entities;

namespace UptimeMonitoring.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Website> Websites => Set<Website>();
    public DbSet<MonitoringResult> MonitoringResults => Set<MonitoringResult>();



}
