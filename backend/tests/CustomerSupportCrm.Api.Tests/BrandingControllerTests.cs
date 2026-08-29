using CustomerSupportCrm.Api.Configuration;
using CustomerSupportCrm.Api.Controllers;
using CustomerSupportCrm.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

public class BrandingControllerTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IBrandingService NewBrandingService(AppDbContext db) =>
        new BrandingService(new RuntimeSettingsService(db, new MemoryCache(new MemoryCacheOptions())));

    private static BrandingController NewController() =>
        new() { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

    [Fact]
    public async Task Get_NoStoredBranding_ReturnsSensibleDefault()
    {
        await using var db = NewDb();

        var result = await NewController().Get(NewBrandingService(db));

        var ok = Assert.IsType<OkObjectResult>(result);
        var settings = Assert.IsType<BrandingSettings>(ok.Value);
        Assert.False(string.IsNullOrWhiteSpace(settings.AppName));
        Assert.Null(settings.LogoDataUrl);
    }

    [Fact]
    public async Task Put_ThenGet_RoundTripsStoredBranding()
    {
        await using var db = NewDb();
        var brandingService = NewBrandingService(db);
        var controller = NewController();

        var putResult = await controller.Put(new BrandingSettings { AppName = "Acme Support", LogoDataUrl = null }, brandingService);
        Assert.IsType<NoContentResult>(putResult);

        var getResult = await controller.Get(brandingService);
        var ok = Assert.IsType<OkObjectResult>(getResult);
        var settings = Assert.IsType<BrandingSettings>(ok.Value);
        Assert.Equal("Acme Support", settings.AppName);
    }

    [Fact]
    public async Task Put_LogoOver256KB_ReturnsBadRequest()
    {
        await using var db = NewDb();
        var brandingService = NewBrandingService(db);
        var controller = NewController();

        // ~400,000 base64 characters decodes to ~300KB, over the 256KB cap.
        var oversizedLogo = "data:image/png;base64," + new string('A', 400_000);
        var result = await controller.Put(new BrandingSettings { AppName = "Acme", LogoDataUrl = oversizedLogo }, brandingService);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Put_MissingAppName_ReturnsBadRequest()
    {
        await using var db = NewDb();
        var brandingService = NewBrandingService(db);
        var controller = NewController();

        var result = await controller.Put(new BrandingSettings { AppName = "  ", LogoDataUrl = null }, brandingService);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Put_InvalidPrimaryColor_ReturnsBadRequest()
    {
        await using var db = NewDb();
        var brandingService = NewBrandingService(db);
        var controller = NewController();

        var result = await controller.Put(new BrandingSettings { AppName = "Acme", PrimaryColorHex = "not-a-color" }, brandingService);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Put_ValidPrimaryColor_PersistsAndRoundTrips()
    {
        await using var db = NewDb();
        var brandingService = NewBrandingService(db);
        var controller = NewController();

        await controller.Put(new BrandingSettings { AppName = "Acme", PrimaryColorHex = "#1E293B" }, brandingService);
        var getResult = await controller.Get(brandingService);

        var ok = Assert.IsType<OkObjectResult>(getResult);
        var settings = Assert.IsType<BrandingSettings>(ok.Value);
        Assert.Equal("#1E293B", settings.PrimaryColorHex);
    }
}
