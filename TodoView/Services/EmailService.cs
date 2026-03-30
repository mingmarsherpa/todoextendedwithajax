using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace TodoView.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendReminderAsync(string toEmail, string message, CancellationToken ct = default)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = "⏰ Your Reminder";

        email.Body = new TextPart("html")
        {
            Text = $"""
                        <div style="font-family: sans-serif; max-width: 600px; margin: auto;">
                            <h2 style="color: #4F46E5;">⏰ Reminder</h2>
                            <p style="font-size: 1.1rem;">{System.Web.HttpUtility.HtmlEncode(message)}</p>
                            <hr/>
                            <small style="color: #888;">Sent by ReminderApp</small>
                        </div>
                    """
        };

        using var smtp = new SmtpClient();

        // SecureSocketOptions.None lets MailPit work without TLS
        var socketOption = _settings.UseAuthentication
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        await smtp.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, socketOption, ct);

        if (_settings.UseAuthentication)
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password, ct);

        await smtp.SendAsync(email, ct);
        await smtp.DisconnectAsync(true, ct);

        _logger.LogInformation("Reminder email sent to {Email}", toEmail);
    }
}