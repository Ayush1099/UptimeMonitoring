using System.ComponentModel.DataAnnotations;

namespace UptimeMonitoring.Application.DTOs;

public class AddWebsiteRequest
{
    [Required(ErrorMessage = "URL is required")]
    [Url(ErrorMessage = "URL must be a valid URL format")]
    public string Url { get; set; } = null!;

    [Range(1, 1440, ErrorMessage = "Check interval must be between 1 and 1440 minutes (24 hours)")]
    public int CheckIntervalMinutes { get; set; } = 5;
}
