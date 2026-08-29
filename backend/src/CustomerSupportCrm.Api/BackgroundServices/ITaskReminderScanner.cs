namespace CustomerSupportCrm.Api.BackgroundServices;

public interface ITaskReminderScanner
{
    // Returns the number of "TaskReminder" notifications created this call - purely for
    // logging by the caller, not used for any control-flow decision.
    Task<int> ScanAndNotifyAsync(DateTime nowUtc, CancellationToken ct);
}
