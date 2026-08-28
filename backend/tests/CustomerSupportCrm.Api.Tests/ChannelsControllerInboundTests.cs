using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Controllers;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using CustomerSupportCrm.Api.Integrations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

// Story 12 fix (Important #1): an inbound webhook must never append a message to a
// ticket whose Source doesn't match the inbound channel, mirroring the existing
// Outbound action's ticket_source_mismatch check.
public class ChannelsControllerInboundTests
{
    private sealed class AlwaysAuthenticated : IInboundWebhookAuthenticator
    {
        public bool Verify(string channel, HttpRequest request) => true;
    }

    private sealed class NoOpDispatcher : IOutboundWebhookDispatcher
    {
        public Task DispatchAsync(string eventType, object payload, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ChannelsController NewController() =>
        new() { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

    [Fact]
    public async Task Inbound_TicketSourceMismatch_ReturnsBadRequest_AndDoesNotPersistMessage()
    {
        await using var db = NewDb();
        var customer = new Customer { FullName = "Existing Customer", Email = "existing@example.com" };
        var ticket = new Ticket { Customer = customer, CustomerId = customer.Id, Subject = "Started via WhatsApp", Source = "WhatsApp" };
        db.Customers.Add(customer);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = NewController();
        var request = new InboundChannelWebhookRequest { From = "someone@example.com", Body = "Reply", TicketId = ticket.Id };

        // An Email-channel webhook attempting to append to a WhatsApp-sourced ticket.
        var result = await controller.Inbound(
            "email", request, db, new AuditLogger(db, NullLogger<AuditLogger>.Instance),
            new AlwaysAuthenticated(), new NoOpDispatcher());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, await db.ChannelMessages.CountAsync());
        var error = Assert.IsType<string>(badRequest.Value!.GetType().GetProperty("error")!.GetValue(badRequest.Value));
        Assert.Equal("ticket_source_mismatch", error);
    }

    [Fact]
    public async Task Inbound_MatchingSource_AppendsMessage()
    {
        await using var db = NewDb();
        var customer = new Customer { FullName = "Existing Customer", Email = "existing@example.com" };
        var ticket = new Ticket { Customer = customer, CustomerId = customer.Id, Subject = "Started via Email", Source = "Email" };
        db.Customers.Add(customer);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = NewController();
        var request = new InboundChannelWebhookRequest { From = "someone@example.com", Body = "Reply", TicketId = ticket.Id };

        var result = await controller.Inbound(
            "email", request, db, new AuditLogger(db, NullLogger<AuditLogger>.Instance),
            new AlwaysAuthenticated(), new NoOpDispatcher());

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, await db.ChannelMessages.CountAsync());
    }
}
