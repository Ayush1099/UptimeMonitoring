namespace UptimeMonitoring.Domain.Entities;

public class Website
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Url { get; set; } = null!;
    public bool IsActive { get; set; }

    public int CheckIntervalMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
}
