using CustomerSupportCrm.Api.BackgroundServices;
using CustomerSupportCrm.Api.Configuration;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

// Direct calls to ITaskReminderScanner.ScanAndNotifyAsync - the scanner is a plain
// constructor-injected service, deliberately split out of TaskReminderBackgroundService so
// this harness can call it directly without any ASP.NET Core hosting machinery.
public class TaskReminderScannerTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ITaskReminderScanner NewScanner(AppDbContext db) =>
        new TaskReminderScanner(db, new RuntimeSettingsService(db, new MemoryCache(new MemoryCacheOptions())));

    private static async Task<(Customer customer, User assignee, Ticket ticket)> SeedTicketAsync(AppDbContext db)
    {
        var customer = new Customer { FullName = "Customer" };
        var assignee = new User { Email = "agent@example.com", DisplayName = "Agent", PasswordHash = "x" };
        db.Customers.Add(customer);
        db.Users.Add(assignee);
        var ticket = new Ticket { CustomerId = customer.Id, Subject = "T", AssignedToUserId = assignee.Id };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        return (customer, assignee, ticket);
    }

    [Fact]
    public async Task ScanAndNotifyAsync_TaskDueWithinLeadWindow_CreatesTaskReminderNotification()
    {
        await using var db = NewDb();
        var (_, assignee, ticket) = await SeedTicketAsync(db);
        var now = DateTime.UtcNow;
        var task = new TicketTask { TicketId = ticket.Id, Title = "Follow up", DueAtUtc = now.AddHours(2), IsDone = false };
        db.TicketTasks.Add(task);
        await db.SaveChangesAsync();

        var created = await NewScanner(db).ScanAndNotifyAsync(now, CancellationToken.None);

        Assert.Equal(1, created);
        var notification = await db.Notifications.SingleAsync(n => n.Type == "TaskReminder");
        Assert.Equal(task.Id, notification.SourceTaskId);
        Assert.Null(notification.SourceTicketNoteId);
        Assert.Equal(assignee.Id, notification.UserId);
        Assert.Equal(ticket.Id, notification.TicketId);
    }

    [Fact]
    public async Task ScanAndNotifyAsync_SecondScan_DoesNotDuplicate()
    {
        await using var db = NewDb();
        var (_, _, ticket) = await SeedTicketAsync(db);
        var now = DateTime.UtcNow;
        db.TicketTasks.Add(new TicketTask { TicketId = ticket.Id, Title = "Follow up", DueAtUtc = now.AddHours(1), IsDone = false });
        await db.SaveChangesAsync();

        var scanner = NewScanner(db);
        var firstRun = await scanner.ScanAndNotifyAsync(now, CancellationToken.None);
        var secondRun = await scanner.ScanAndNotifyAsync(now.AddMinutes(1), CancellationToken.None);

        Assert.Equal(1, firstRun);
        Assert.Equal(0, secondRun);
        Assert.Equal(1, await db.Notifications.CountAsync(n => n.Type == "TaskReminder"));
    }

    [Fact]
    public async Task ScanAndNotifyAsync_DoneTask_IsSkipped()
    {
        await using var db = NewDb();
        var (_, _, ticket) = await SeedTicketAsync(db);
        var now = DateTime.UtcNow;
        db.TicketTasks.Add(new TicketTask { TicketId = ticket.Id, Title = "Already done", DueAtUtc = now.AddHours(1), IsDone = true });
        await db.SaveChangesAsync();

        var created = await NewScanner(db).ScanAndNotifyAsync(now, CancellationToken.None);

        Assert.Equal(0, created);
        Assert.Equal(0, await db.Notifications.CountAsync(n => n.Type == "TaskReminder"));
    }

    [Fact]
    public async Task ScanAndNotifyAsync_TaskOutsideLeadWindow_IsSkipped()
    {
        await using var db = NewDb();
        var (_, _, ticket) = await SeedTicketAsync(db);
        var now = DateTime.UtcNow;
        // Default lead time is 24h - a task due in 3 days is not yet within the window.
        db.TicketTasks.Add(new TicketTask { TicketId = ticket.Id, Title = "Far off", DueAtUtc = now.AddDays(3), IsDone = false });
        await db.SaveChangesAsync();

        var created = await NewScanner(db).ScanAndNotifyAsync(now, CancellationToken.None);

        Assert.Equal(0, created);
    }

    [Fact]
    public async Task ScanAndNotifyAsync_UnassignedTicket_CreatesNoNotification_ButIsNotPermanentlySkipped()
    {
        await using var db = NewDb();
        var customer = new Customer { FullName = "Customer" };
        db.Customers.Add(customer);
        var ticket = new Ticket { CustomerId = customer.Id, Subject = "Unassigned", AssignedToUserId = null };
        db.Tickets.Add(ticket);
        var now = DateTime.UtcNow;
        var task = new TicketTask { TicketId = ticket.Id, Title = "Follow up", DueAtUtc = now.AddHours(1), IsDone = false };
        db.TicketTasks.Add(task);
        await db.SaveChangesAsync();

        var scanner = NewScanner(db);
        var created = await scanner.ScanAndNotifyAsync(now, CancellationToken.None);
        Assert.Equal(0, created);

        // Once assigned, the very next scan picks it up - it was never marked "reminded".
        ticket.AssignedToUserId = (await db.Users.AddAsync(new User { Email = "later@example.com", DisplayName = "Later", PasswordHash = "x" })).Entity.Id;
        await db.SaveChangesAsync();
        var secondRun = await scanner.ScanAndNotifyAsync(now.AddMinutes(1), CancellationToken.None);

        Assert.Equal(1, secondRun);
    }

    [Fact]
    public async Task ScanAndNotifyAsync_NeverSetsBothSourceTaskIdAndSourceTicketNoteId()
    {
        await using var db = NewDb();
        var (_, _, ticket) = await SeedTicketAsync(db);
        var now = DateTime.UtcNow;
        db.TicketTasks.Add(new TicketTask { TicketId = ticket.Id, Title = "Follow up", DueAtUtc = now.AddHours(1), IsDone = false });
        await db.SaveChangesAsync();

        await NewScanner(db).ScanAndNotifyAsync(now, CancellationToken.None);

        var notification = await db.Notifications.SingleAsync(n => n.Type == "TaskReminder");
        Assert.True(notification.SourceTaskId.HasValue);
        Assert.Null(notification.SourceTicketNoteId);
    }
}
