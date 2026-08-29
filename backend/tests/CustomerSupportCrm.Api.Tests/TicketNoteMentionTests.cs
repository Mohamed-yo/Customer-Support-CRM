using CustomerSupportCrm.Api.Controllers;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

public class TicketNoteMentionTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static TicketsController NewController(Guid actorUserId)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[] { new System.Security.Claims.Claim("sub", actorUserId.ToString()) }, "TestAuth"));
        return new TicketsController { ControllerContext = new ControllerContext { HttpContext = httpContext } };
    }

    private static async Task<(Ticket ticket, Guid authorId)> SeedTicketAsync(AppDbContext db)
    {
        var customer = new Customer { FullName = "Customer" };
        var author = new User { Email = "author@example.com", DisplayName = "Author", PasswordHash = "x", IsActive = true };
        db.Customers.Add(customer);
        db.Users.Add(author);
        var ticket = new Ticket { CustomerId = customer.Id, Subject = "T" };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        return (ticket, author.Id);
    }

    [Fact]
    public async Task CreateNote_WithMention_CreatesMentionNotification()
    {
        await using var db = NewDb();
        var (ticket, authorId) = await SeedTicketAsync(db);
        var mentioned = new User { Email = "mentioned@example.com", DisplayName = "Mentioned", PasswordHash = "x", IsActive = true };
        db.Users.Add(mentioned);
        await db.SaveChangesAsync();

        var controller = NewController(authorId);
        var result = await controller.CreateNote(
            ticket.Id, new TicketNoteCreateRequest { Body = "cc @Mentioned", MentionedUserIds = new[] { mentioned.Id } }, db);

        Assert.IsType<CreatedAtActionResult>(result);
        var notification = await db.Notifications.SingleAsync(n => n.Type == "Mention");
        Assert.Equal(mentioned.Id, notification.UserId);
        Assert.Equal(ticket.Id, notification.TicketId);
        Assert.Null(notification.SourceTaskId);
        Assert.NotNull(notification.SourceTicketNoteId);
    }

    [Fact]
    public async Task CreateNote_UnknownMentionedUserId_ReturnsBadRequest()
    {
        await using var db = NewDb();
        var (ticket, authorId) = await SeedTicketAsync(db);

        var controller = NewController(authorId);
        var result = await controller.CreateNote(
            ticket.Id, new TicketNoteCreateRequest { Body = "cc @Ghost", MentionedUserIds = new[] { Guid.NewGuid() } }, db);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, await db.TicketNotes.CountAsync());
        Assert.Equal(0, await db.Notifications.CountAsync());
    }

    [Fact]
    public async Task CreateNote_DeactivatedMentionedUserId_ReturnsBadRequest()
    {
        await using var db = NewDb();
        var (ticket, authorId) = await SeedTicketAsync(db);
        var deactivated = new User { Email = "gone@example.com", DisplayName = "Gone", PasswordHash = "x", IsActive = false };
        db.Users.Add(deactivated);
        await db.SaveChangesAsync();

        var controller = NewController(authorId);
        var result = await controller.CreateNote(
            ticket.Id, new TicketNoteCreateRequest { Body = "cc @Gone", MentionedUserIds = new[] { deactivated.Id } }, db);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, await db.Notifications.CountAsync());
    }

    [Fact]
    public async Task CreateNote_MentioningSelf_CreatesNoNotification_ButStillSucceeds()
    {
        await using var db = NewDb();
        var (ticket, authorId) = await SeedTicketAsync(db);

        var controller = NewController(authorId);
        var result = await controller.CreateNote(
            ticket.Id, new TicketNoteCreateRequest { Body = "note to self", MentionedUserIds = new[] { authorId } }, db);

        Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(0, await db.Notifications.CountAsync());
    }
}
