using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace LiftAI.Api.Services.Email;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _opts = options.Value;

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        using var client = new SmtpClient(_opts.Host, _opts.Port);
        client.EnableSsl = _opts.EnableSsl;
        client.Credentials = new NetworkCredential(_opts.User, _opts.Password);

        var message = new MailMessage
        {
            From = new MailAddress(_opts.From),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message);
    }
}