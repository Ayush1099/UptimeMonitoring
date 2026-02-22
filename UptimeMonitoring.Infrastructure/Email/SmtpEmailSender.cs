using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UptimeMonitoring.Application.Interfaces;

namespace UptimeMonitoring.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var enabled = _configuration.GetValue<bool?>("Smtp:Enabled") ?? false;
        if (!enabled)
        {
            _logger.LogInformation("SMTP disabled; skipping email to {To}", to);
            return;
        }

        var host = _configuration["Smtp:Host"];
        var port = _configuration.GetValue<int?>("Smtp:Port") ?? 587;
        var enableSsl = _configuration.GetValue<bool?>("Smtp:EnableSsl") ?? true;
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var from = _configuration["Smtp:From"];

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(from))
        {
            _logger.LogWarning("SMTP enabled but not fully configured; skipping email send.");
            return;
        }

        using var message = new MailMessage(from: from, to: to, subject, body);

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = enableSsl
        };

        // SmtpClient doesn't support CancellationToken directly everywhere; honor it best-effort.
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message);
    }
}
