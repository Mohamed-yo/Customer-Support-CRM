namespace CustomerSupportCrm.Api.BackgroundServices;

// Thin timer wrapper - all business logic lives in ITaskReminderScanner (constructor-testable
// in isolation, with no ASP.NET Core hosting machinery involved).
public class TaskReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TaskReminderBackgroundService> _logger;

    public TaskReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<TaskReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(ScanInterval);
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scanner = scope.ServiceProvider.GetRequiredService<ITaskReminderScanner>();
                var created = await scanner.ScanAndNotifyAsync(DateTime.UtcNow, stoppingToken);
                if (created > 0)
                {
                    _logger.LogInformation("Task reminder scan created {Count} notification(s).", created);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One failed scan (e.g. a transient DB hiccup) must never take the whole
                // process down or stop future scans.
                _logger.LogWarning(ex, "Task reminder scan failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
