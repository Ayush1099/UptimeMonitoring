namespace UptimeMonitoring.Application.DTOs;

public class WebsiteResponse
{
    public Guid Id { get; set; }
    public string Url { get; set; } = null!;
    public bool IsActive { get; set; }
    public int CheckIntervalMinutes { get; set; }
}
