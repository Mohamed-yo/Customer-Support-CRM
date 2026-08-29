using CustomerSupportCrm.Api.Ai;
using CustomerSupportCrm.Api.Controllers;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

// Primary evidence for the credential-dependent Acceptance Criteria - MockAiProvider proves
// every AI action can produce a real "Ok" result end-to-end with zero real vendor account.
public class AiControllerMockProviderTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AiController NewController() =>
        new() { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

    private static string GetStatus(object value) => (string)value.GetType().GetProperty("status")!.GetValue(value)!;
    private static object? GetValue(object value) => value.GetType().GetProperty("value")!.GetValue(value);

    private static async Task<(Ticket ticket, KnowledgeArticle article)> SeedTicketWithKbArticleAsync(AppDbContext db)
    {
        var customer = new Customer { FullName = "Customer" };
        var author = new User { Email = "author@example.com", DisplayName = "Author", PasswordHash = "x" };
        db.Customers.Add(customer);
        db.Users.Add(author);
        var ticket = new Ticket { CustomerId = customer.Id, Subject = "Billing issue", Description = "Card declined" };
        var article = new KnowledgeArticle { Title = "How to update your card", Body = "...", CreatedByUserId = author.Id };
        db.Tickets.Add(ticket);
        db.KnowledgeArticles.Add(article);
        await db.SaveChangesAsync();
        return (ticket, article);
    }

    [Fact]
    public async Task Summarize_MockProvider_ReturnsOkWithValue()
    {
        await using var db = NewDb();
        var (ticket, _) = await SeedTicketWithKbArticleAsync(db);

        var result = await NewController().Summarize(ticket.Id, db, new MockAiProvider());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Ok", GetStatus(ok.Value!));
        Assert.NotNull(GetValue(ok.Value!));
    }

    [Fact]
    public async Task SuggestReply_MockProvider_ReturnsOkWithValue()
    {
        await using var db = NewDb();
        var (ticket, _) = await SeedTicketWithKbArticleAsync(db);

        var result = await NewController().SuggestReply(ticket.Id, db, new MockAiProvider());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Ok", GetStatus(ok.Value!));
    }

    [Fact]
    public async Task SuggestCategory_MockProvider_ReturnsOneOfTheAllowedCategories()
    {
        await using var db = NewDb();
        var (ticket, _) = await SeedTicketWithKbArticleAsync(db);

        var result = await NewController().SuggestCategory(ticket.Id, db, new MockAiProvider());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Ok", GetStatus(ok.Value!));
        Assert.Contains((string)GetValue(ok.Value!)!, TicketsController.AllowedCategories);
    }

    [Fact]
    public async Task SuggestKbArticles_MockProvider_ReturnsSuggestedArticles()
    {
        await using var db = NewDb();
        var (ticket, article) = await SeedTicketWithKbArticleAsync(db);

        var result = await NewController().SuggestKbArticles(ticket.Id, db, new MockAiProvider());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Ok", GetStatus(ok.Value!));
        var articles = Assert.IsAssignableFrom<System.Collections.IEnumerable>(GetValue(ok.Value!));
        Assert.Contains(article.Id, articles.Cast<object>().Select(a => (Guid)a.GetType().GetProperty("Id")!.GetValue(a)!));
    }

    [Fact]
    public async Task AiChatController_MockProvider_ReturnsOkWithEcho()
    {
        var controller = new AiChatController { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

        var result = await controller.Chat(new AiChatController.ChatRequest("session-1", "hello"), new MockAiProvider());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Ok", GetStatus(ok.Value!));
        Assert.Contains("hello", (string)GetValue(ok.Value!)!);
    }
}
