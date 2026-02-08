using System.ComponentModel.DataAnnotations;

namespace UptimeMonitoring.Application.DTOs;

public class WebsiteActionRequest
{
    [Required(ErrorMessage = "URL is required")]
    [Url(ErrorMessage = "URL must be a valid URL format")]
    public string Url { get; set; } = null!;
}
