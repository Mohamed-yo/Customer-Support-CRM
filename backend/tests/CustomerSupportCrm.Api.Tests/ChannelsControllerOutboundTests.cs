using CustomerSupportCrm.Api.Controllers;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using CustomerSupportCrm.Api.Integrations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

// Covers the ripple from the Important #2 fix: Customer.Email is no longer guaranteed
// non-null, so Outbound's WhatsApp/SMS reply path (which used to fall back to Email) must
// explicitly reject a reply when the channel-appropriate contact address is missing,
// rather than sending to null/empty.
public class ChannelsControllerOutboundTests
{
    private sealed class FakeSender : IChannelSender
    {
        public Task<SendResult> SendAsync(string to, string? subject, string body, CancellationToken ct = default) =>
            Task.FromResult(new SendResult(SendStatus.Success));
    }

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IServiceProvider NewServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IChannelSender>("Email", new FakeSender());
        services.AddKeyedSingleton<IChannelSender>("WhatsApp", new FakeSender());
        services.AddKeyedSingleton<IChannelSender>("SMS", new FakeSender());
        return services.BuildServiceProvider();
    }

    private static ChannelsController NewController() =>
        new() { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

    [Fact]
    public async Task Outbound_WhatsApp_NoPhoneOnFile_ReturnsBadRequest_NotNullAddress()
    {
        await using var db = NewDb();
        // A WhatsApp-sourced customer with no Phone on file is an inconsistent/edge state,
        // but must never reach the sender with a null "to" address.
        var customer = new Customer { FullName = "No Phone", Email = null, Phone = null };
        var ticket = new Ticket { Customer = customer, CustomerId = customer.Id, Subject = "Via WhatsApp", Source = "WhatsApp" };
        db.Customers.Add(customer);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = NewController();
        var request = new OutboundChannelReplyRequest { TicketId = ticket.Id, Body = "Reply" };

        var result = await controller.Outbound("whatsapp", request, db, NewServiceProvider());

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, await db.ChannelMessages.CountAsync());
    }

    [Fact]
    public async Task Outbound_WhatsApp_UsesCustomerPhone_NotEmail()
    {
        await using var db = NewDb();
        var customer = new Customer { FullName = "Phone Customer", Email = null, Phone = "+15551234567" };
        var ticket = new Ticket { Customer = customer, CustomerId = customer.Id, Subject = "Via WhatsApp", Source = "WhatsApp" };
        db.Customers.Add(customer);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var controller = NewController();
        var request = new OutboundChannelReplyRequest { TicketId = ticket.Id, Body = "Reply" };

        var result = await controller.Outbound("whatsapp", request, db, NewServiceProvider());

        var ok = Assert.IsType<OkObjectResult>(result);
        var item = Assert.IsType<ChannelMessageItem>(ok.Value);
        Assert.Equal("+15551234567", item.ToAddress);
    }
}
