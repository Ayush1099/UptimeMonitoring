namespace UptimeMonitoring.Domain.Entities;

public class MonitoringResult
{
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }

    public bool IsUp { get; set; }
    public int ResponseTimeMs { get; set; }

    public DateTime CheckedAt { get; set; }
}
