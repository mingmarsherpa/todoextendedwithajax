namespace TodoView.Services;

public interface IEmailService
{
    Task SendReminderAsync(string toEmail, string message, CancellationToken ct = default);
}