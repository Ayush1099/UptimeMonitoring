namespace UptimeMonitoring.Application.DTOs;

public class AddWebsiteRequest
{
    public string Url { get; set; } = null!;
    public int CheckIntervalMinutes { get; set; } = 5;
}
