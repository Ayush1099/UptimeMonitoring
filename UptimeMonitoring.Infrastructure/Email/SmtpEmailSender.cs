using System.Net;
using System.Net.Mail;

namespace UptimeMonitoring.Infrastructure.Email;

public class SmtpEmailSender
{
    public async Task SendAsync(string to, string subject, string body)
    {
        var message = new MailMessage(
            from: "ayush99verma@gmail.com",
            to: "ayush99verma@gmail.com",
            subject,
            body
        );

        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            Credentials = new NetworkCredential(
                "ayush99verma@gmail.com",
                "hvqs wmnf pbqp svxn"
            ),
            EnableSsl = true
        };

        await client.SendMailAsync(message);
    }
}
