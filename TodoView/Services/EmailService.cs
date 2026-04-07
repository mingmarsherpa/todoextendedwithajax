using Resend;
using Microsoft.Extensions.Options;

namespace TodoView.Services;

public class EmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IResend resend,IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _resend = resend;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendReminderAsync(string toEmail, string message, CancellationToken ct = default)
    {
        var email = new EmailMessage();
        email.From = $"{_settings.FromName} <{_settings.FromAddress}>";
        email.To.Add(toEmail);
        email.Subject = "⏰ Your Reminder";
        email.HtmlBody = $"""
                          <div style="font-family: sans-serif; max-width: 600px; margin: auto;">
                              <h2 style="color: #4F46E5;">⏰ Reminder</h2>
                              <p style="font-size: 1.1rem;">{System.Web.HttpUtility.HtmlEncode(message)}</p>
                              <hr/>
                              <small style="color: #888;">Sent by TodoView</small>
                          </div>
                          """;

        await _resend.EmailSendAsync(email);

        _logger.LogInformation("Reminder email sent to {Email}", toEmail);
    }
}