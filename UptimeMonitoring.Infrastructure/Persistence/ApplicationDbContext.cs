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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Website>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Url).IsRequired();
        });

        modelBuilder.Entity<MonitoringResult>(entity =>
        {
            entity.HasKey(x => x.Id);
        });
    }

}
