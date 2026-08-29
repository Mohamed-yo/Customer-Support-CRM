using CustomerSupportCrm.Api.Ai;
using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Configuration;
using CustomerSupportCrm.Api.Controllers;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using CustomerSupportCrm.Api.Integrations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

// Primary evidence for the credential-independent Acceptance Criteria - every AI action
// degrades to NotConfigured with zero real vendor credentials, and never blocks the
// underlying manual ticket workflow.
public class AiControllerNullProviderTests
{
    private sealed class NoOpDispatcher : IOutboundWebhookDispatcher
    {
        public Task DispatchAsync(string eventType, object payload, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AiController NewController() =>
        new() { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

    private static string GetStatus(object value) => (string)value.GetType().GetProperty("status")!.GetValue(value)!;
    private static object? GetValue(object value) => value.GetType().GetProperty("value")!.GetValue(value);

    private static async Task<Ticket> SeedTicketWithNoteAsync(AppDbContext db)
    {
        var customer = new Customer { FullName = "Customer" };
        var author = new User { Email = "author@example.com", DisplayName = "Author", PasswordHash = "x" };
        db.Customers.Add(customer);
        db.Users.Add(author);
        var ticket = new Ticket { CustomerId = customer.Id, Subject = "Billing issue", Description = "Card declined" };
        db.Tickets.Add(ticket);
        db.TicketNotes.Add(new TicketNote { TicketId = ticket.Id, AuthorUserId = author.Id, Body = "Investigating" });
        await db.SaveChangesAsync();
        return ticket;
    }

    [Fact]
    public async Task Summarize_NullProvider_ReturnsNotConfigured()
    {
        await using var db = NewDb();
        var ticket = await SeedTicketWithNoteAsync(db);

        var result = await NewController().Summarize(ticket.Id, db, new NullAiProvider());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("NotConfigured", GetStatus(ok.Value!));
        Assert.Null(GetValue(ok.Value!));
    }

    [Fact]
    public async Task SuggestReply_NullProvider_ReturnsNotConfigured()
    {
        await using var db = NewDb();
        var ticket = await SeedTicketWithNoteAsync(db);

        var result = await NewController().SuggestReply(ticket.Id, db, new NullAiProvider());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("NotConfigured", GetStatus(ok.Value!));
    }

    [Fact]
    public async Task SuggestCategory_NullProvider_ReturnsNotConfigured()
    {
        await using var db = NewDb();
        var ticket = await SeedTicketWithNoteAsync(db);

        var result = await NewController().SuggestCategory(ticket.Id, db, new NullAiProvider());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("NotConfigured", GetStatus(ok.Value!));
    }

    [Fact]
    public async Task SuggestKbArticles_NullProvider_ReturnsNotConfigured()
    {
        await using var db = NewDb();
        var ticket = await SeedTicketWithNoteAsync(db);

        var result = await NewController().SuggestKbArticles(ticket.Id, db, new NullAiProvider());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("NotConfigured", GetStatus(ok.Value!));
    }

    [Fact]
    public async Task AiChatController_NullProvider_ReturnsNotConfigured()
    {
        var controller = new AiChatController { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

        var result = await controller.Chat(new AiChatController.ChatRequest("session-1", "hello"), new NullAiProvider());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("NotConfigured", GetStatus(ok.Value!));
    }

    [Fact]
    public async Task AiActions_WithNullProvider_DoNotBlockUnderlyingTicketWorkflow()
    {
        await using var db = NewDb();
        var ticket = await SeedTicketWithNoteAsync(db);
        await NewController().Summarize(ticket.Id, db, new NullAiProvider());
        await NewController().SuggestReply(ticket.Id, db, new NullAiProvider());
        await NewController().SuggestCategory(ticket.Id, db, new NullAiProvider());

        var runtimeSettings = new RuntimeSettingsService(db, new MemoryCache(new MemoryCacheOptions()));
        var ticketsController = new TicketsController { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var request = new TicketUpsertRequest { CustomerId = ticket.CustomerId, Subject = "Unaffected ticket", Status = "Open", Category = "General", Priority = "Normal" };

        var result = await ticketsController.Create(
            request, db, new AuditLogger(db, NullLogger<AuditLogger>.Instance), new NoOpDispatcher(), runtimeSettings);

        Assert.IsType<CreatedAtActionResult>(result);
    }
}
