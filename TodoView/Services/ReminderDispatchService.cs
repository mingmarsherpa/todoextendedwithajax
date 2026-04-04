using Hangfire;
using Microsoft.EntityFrameworkCore;
using TodoView.Data;

namespace TodoView.Services;

public class ReminderDispatchService
{
    private readonly TodoDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ReminderDispatchService> _logger;

    public ReminderDispatchService(
        TodoDbContext context,
        IEmailService emailService,
        ILogger<ReminderDispatchService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task SendReminderAsync(int todoId, string toEmail, string message, CancellationToken ct = default)
    {
        var todo = await _context.TodoItems.FirstOrDefaultAsync(t => t.Id == todoId, ct);
        if (todo is null)
        {
            _logger.LogInformation("Skipping reminder for missing task {TodoId}.", todoId);
            return;
        }

        if (todo.IsDone || todo.ReminderAt is null || todo.ReminderTriggeredAt.HasValue)
        {
            _logger.LogInformation("Skipping reminder for task {TodoId} because it is no longer pending.", todoId);
            todo.HangfireJobId = null;
            await _context.SaveChangesAsync(ct);
            return;
        }

        await _emailService.SendReminderAsync(toEmail, message, ct);

        todo.ReminderTriggeredAt = DateTime.UtcNow;
        todo.HangfireJobId = null;
        await _context.SaveChangesAsync(ct);
    }
}
