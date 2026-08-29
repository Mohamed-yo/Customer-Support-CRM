using CustomerSupportCrm.Api.Configuration;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.BackgroundServices;

// Plain application service (constructor-injected, not [FromServices] per-action) - a
// scoped scanner invoked by TaskReminderBackgroundService's timer, not by any controller.
public class TaskReminderScanner : ITaskReminderScanner
{
    private readonly AppDbContext _db;
    private readonly IRuntimeSettings _runtimeSettings;

    public TaskReminderScanner(AppDbContext db, IRuntimeSettings runtimeSettings)
    {
        _db = db;
        _runtimeSettings = runtimeSettings;
    }

    public async Task<int> ScanAndNotifyAsync(DateTime nowUtc, CancellationToken ct)
    {
        var leadTime = await _runtimeSettings.GetAsync(RuntimeSettingKeys.ReminderLeadHrs, new ReminderLeadTimeSetting(), ct);
        var horizon = nowUtc.AddHours(leadTime.Hours);

        var candidates = await _db.TicketTasks
            .Where(t => !t.IsDone && t.DueAtUtc != null && t.DueAtUtc <= horizon)
            .Select(t => new { t.Id, t.TicketId, t.Title })
            .ToListAsync(ct);

        if (candidates.Count == 0) return 0;

        // Best-effort pre-filter - the filtered unique index on Notification.SourceTaskId is
        // the actual exactly-once guarantee, not this query (a concurrent scan could race it).
        var taskIds = candidates.Select(c => c.Id).ToList();
        var alreadyReminded = await _db.Notifications
            .Where(n => n.Type == "TaskReminder" && n.SourceTaskId != null && taskIds.Contains(n.SourceTaskId.Value))
            .Select(n => n.SourceTaskId!.Value)
            .ToListAsync(ct);
        var alreadyRemindedSet = alreadyReminded.ToHashSet();

        var pending = candidates.Where(c => !alreadyRemindedSet.Contains(c.Id)).ToList();
        if (pending.Count == 0) return 0;

        var ticketIds = pending.Select(c => c.TicketId).Distinct().ToList();
        var assigneeByTicketId = await _db.Tickets
            .Where(t => ticketIds.Contains(t.Id) && t.AssignedToUserId != null)
            .Select(t => new { t.Id, AssigneeId = t.AssignedToUserId!.Value })
            .ToDictionaryAsync(t => t.Id, t => t.AssigneeId, ct);

        var created = 0;
        foreach (var task in pending)
        {
            // No assignee - nobody to remind. It will be picked up again next scan if/when
            // the ticket gets assigned, since it's still absent from alreadyRemindedSet.
            if (!assigneeByTicketId.TryGetValue(task.TicketId, out var assigneeId)) continue;

            _db.Notifications.Add(new Notification
            {
                UserId = assigneeId,
                TicketId = task.TicketId,
                Type = "TaskReminder",
                Message = $"Reminder: \"{task.Title}\" is due soon.",
                IsRead = false,
                SourceTaskId = task.Id,
            });
            created++;
        }

        if (created == 0) return 0;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // The filtered unique index rejected a duplicate - a concurrent scan already
            // reminded one of these tasks. Not an error worth surfacing; the next tick's
            // pre-filter will see it as already reminded and skip it.
            return 0;
        }

        return created;
    }
}
