public class DashboardWebsiteStatusResponse
{
    public Guid WebsiteId { get; set; }
    public string Url { get; set; } = null!;
    public string Status { get; set; } = "UNKNOWN";
    public DateTime? LastCheckedAt { get; set; }
    public int? ResponseTimeMs { get; set; }
    public double? UptimePercentage { get; set; }

}
