using Hangfire;

namespace TodoView.Services;

public class ReminderScheduler
{
    private readonly IBackgroundJobClient _jobs;

    public ReminderScheduler(IBackgroundJobClient jobs) => _jobs = jobs;

    public string Schedule(int todoId, string email, string message, DateTimeOffset sendAt)
    {
        var delay = sendAt - DateTimeOffset.UtcNow;
        return _jobs.Schedule<ReminderDispatchService>(
            svc => svc.SendReminderAsync(todoId, email, message, CancellationToken.None),
            delay > TimeSpan.Zero ? delay : TimeSpan.Zero
        );
    }
}
