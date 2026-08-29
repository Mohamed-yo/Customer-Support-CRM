using System.Security.Claims;
using CustomerSupportCrm.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CustomerSupportCrm.Api.Tests;

// Direct action-call test: simulates an already-authenticated principal (as the ApiKey
// scheme would have produced) and asserts only the controller's own response-shaping
// logic. It does NOT exercise the ApiKey scheme, the RequireExternalClient policy, or the
// rate limiter - those are pipeline-level and are verified manually (see the story
// plan's Manual/Runtime Verification section).
public class PublicApiControllerTests
{
    [Fact]
    public void Ping_ReturnsCallingKeysLabelAndServerTime()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, "Partner Integration"),
                new Claim("external_client", "true"),
            },
            authenticationType: "ApiKey",
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role));

        var controller = new PublicApiController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            },
        };

        var before = DateTime.UtcNow;
        var result = controller.Ping();
        var after = DateTime.UtcNow;

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<PublicApiController.PingResponse>(ok.Value);

        Assert.Equal("Partner Integration", body.Label);
        Assert.InRange(body.ServerTimeUtc, before, after);
    }
}
