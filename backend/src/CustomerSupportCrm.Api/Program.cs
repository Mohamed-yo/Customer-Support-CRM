using CustomerSupportCrm.Api.Ai;
using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Auth;
using CustomerSupportCrm.Api.BackgroundServices;
using CustomerSupportCrm.Api.Configuration;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using CustomerSupportCrm.Api.Hubs;
using CustomerSupportCrm.Api.Integrations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Story 24: lets Swagger UI (Development-only, unchanged hosting model) exercise the
    // X-Api-Key-protected endpoints interactively. Purely additive - no existing JWT
    // bearer scheme definition existed before this, and none is added now; this only adds
    // documentation/testability for the new scheme.
    options.AddSecurityDefinition(ApiKeyAuthenticationHandler.SchemeName, new OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "API key issued via /api/api-keys. Send as the X-Api-Key header.",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = ApiKeyAuthenticationHandler.SchemeName },
            },
            Array.Empty<string>()
        },
    });
});

builder.Services.AddSingleton<PasswordHasher<User>>();
builder.Services.AddSingleton<PasswordHasher<Customer>>();
// Story 24: hashes API-key secrets the same way passwords are hashed elsewhere in this
// codebase - PasswordHasher<T>'s generic parameter is just a type marker, not a
// requirement that T represent a login-capable user.
builder.Services.AddSingleton<PasswordHasher<ApiKey>>();
builder.Services.AddSingleton<JwtTokenService>();
// Scoped: AuditLogger takes a scoped AppDbContext. A targeted writer at the two
// known mutation points (login, role assignment) is used instead of a generic
// SaveChanges interceptor, which would also audit unrelated calls (e.g. the dev seed).
builder.Services.AddScoped<AuditLogger>();

// Story 12: channel/integration configuration - blank in the committed appsettings.json
// (see Jwt:SigningKey precedent), real values only in appsettings.Development.json.
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<ChannelInboundSecrets>(builder.Configuration.GetSection("Channels:InboundSecrets"));
builder.Services.Configure<WhatsappOutboundOptions>(builder.Configuration.GetSection("Channels:WhatsappOutbound"));
builder.Services.Configure<SmsOutboundOptions>(builder.Configuration.GetSection("Channels:SmsOutbound"));

builder.Services.AddScoped<EmailSender>();
builder.Services.AddKeyedScoped<IChannelSender>("Email", (sp, _) => sp.GetRequiredService<EmailSender>());
builder.Services.AddHttpClient<WhatsappSender>();
builder.Services.AddKeyedScoped<IChannelSender>("WhatsApp", (sp, _) => sp.GetRequiredService<WhatsappSender>());
builder.Services.AddHttpClient<SmsSender>();
builder.Services.AddKeyedScoped<IChannelSender>("SMS", (sp, _) => sp.GetRequiredService<SmsSender>());

builder.Services.AddScoped<IInboundWebhookAuthenticator, SharedSecretAuthenticator>();

// Story 15: admin-editable runtime config (SLA targets, reminder lead time, branding) backed
// by the RuntimeSetting table, cached briefly via IMemoryCache to keep hot paths (ticket
// SLA computation) off the database.
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IRuntimeSettings, RuntimeSettingsService>();
builder.Services.AddScoped<IBrandingService, BrandingService>();

// Story 15 Phase 5: periodic scan for tasks nearing their due date - see
// TaskReminderScanner's own summary for the exactly-once guarantee.
builder.Services.AddScoped<ITaskReminderScanner, TaskReminderScanner>();
builder.Services.AddHostedService<TaskReminderBackgroundService>();

// Story 15 Phase 6: provider-agnostic AI abstraction. "none" (default, blank config) ships
// with zero real vendor credentials required - every AI feature degrades to NotConfigured.
// "mock" is a deterministic stand-in for verifying the "configured" acceptance criteria
// without any real vendor. Neither branch names an actual AI vendor SDK/type.
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("Ai"));
builder.Services.AddScoped<IAiProvider>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value;
    return options.Provider.Trim().ToLowerInvariant() switch
    {
        "mock" => new MockAiProvider(),
        _ => new NullAiProvider(),
    };
});

builder.Services.AddHttpClient<OutboundWebhookDispatcher>();
builder.Services.AddScoped<IOutboundWebhookDispatcher>(sp => sp.GetRequiredService<OutboundWebhookDispatcher>());

builder.Services.AddSignalR();

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim types exactly as issued (e.g. "sub") instead of ASP.NET Core's
        // default inbound mapping to long XML-namespaced claim type URIs.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwt["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey missing"))),
        };
        // Story 12: SignalR's browser client can't always set a custom Authorization
        // header on the WebSocket handshake, so accept the token via query string for
        // hub requests only. Purely additive - every other request still authenticates
        // via the Authorization header exactly as before.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    })
    // Story 24: a third, additive scheme for external clients. JWT bearer stays the
    // default scheme above - every existing endpoint's authentication is unaffected.
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization(options =>
{
    // The two identity kinds issued by JwtTokenService ("staff" vs "customer") are kept
    // strictly separated by this claim - a customer token must never satisfy a staff-only
    // endpoint and vice versa, regardless of any role claims present.
    options.AddPolicy("RequireStaff", p => p.RequireAuthenticatedUser().RequireClaim("type", "staff"));
    options.AddPolicy("RequireCustomer", p => p.RequireAuthenticatedUser().RequireClaim("type", "customer"));
    // Story 24: satisfied only by the ApiKey scheme above - a staff/customer JWT is never
    // evaluated against this policy (AddAuthenticationSchemes restricts which scheme(s)
    // this policy accepts), and an API key never carries the "type" claim the two
    // policies above require, so it can never satisfy them either.
    options.AddPolicy("RequireExternalClient", p => p
        .AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName)
        .RequireAuthenticatedUser()
        .RequireClaim("external_client", "true"));
});

// Story 24: built into the net8.0 shared framework - no new package. Partitioned per API
// key so one client's usage can never exhaust another's allowance; only applies to
// endpoints attributed with [EnableRateLimiting("ApiKeyPolicy")] (PublicApiController) -
// every existing JWT-authenticated endpoint is untouched.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("ApiKeyPolicy", context =>
    {
        var keyId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(keyId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    try
    {
        using var scope = app.Services.CreateScope();
        var seedDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedData.EnsureSeedRolesAsync(seedDb);
        await SeedData.EnsureSeedUserAsync(
            seedDb,
            scope.ServiceProvider.GetRequiredService<PasswordHasher<User>>());
    }
    catch (Exception ex)
    {
        // Dev-only convenience seed — must never block startup or take down /api/health
        // when the database isn't reachable yet (e.g. before the first migration is applied).
        app.Logger.LogWarning(ex, "Skipping dev seed user: database not reachable yet.");
    }
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();

app.Run();
