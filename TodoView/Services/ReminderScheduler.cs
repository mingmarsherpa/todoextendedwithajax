using Hangfire;

namespace TodoView.Services;

public class ReminderScheduler
{
    private readonly IBackgroundJobClient _jobs;

    public ReminderScheduler(IBackgroundJobClient jobs) => _jobs = jobs;

    public string Schedule(string email, string message, DateTimeOffset sendAt)
    {
        var delay = sendAt - DateTimeOffset.UtcNow;
        return _jobs.Schedule<IEmailService>(
            svc => svc.SendReminderAsync(email, message, CancellationToken.None),
            delay > TimeSpan.Zero ? delay : TimeSpan.Zero
        );
    }
}