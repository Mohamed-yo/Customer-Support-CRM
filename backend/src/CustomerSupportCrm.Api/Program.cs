using CustomerSupportCrm.Api.Auditing;
using CustomerSupportCrm.Api.Auth;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using CustomerSupportCrm.Api.Hubs;
using CustomerSupportCrm.Api.Integrations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<PasswordHasher<User>>();
builder.Services.AddSingleton<PasswordHasher<Customer>>();
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
    });
builder.Services.AddAuthorization(options =>
{
    // The two identity kinds issued by JwtTokenService ("staff" vs "customer") are kept
    // strictly separated by this claim - a customer token must never satisfy a staff-only
    // endpoint and vice versa, regardless of any role claims present.
    options.AddPolicy("RequireStaff", p => p.RequireAuthenticatedUser().RequireClaim("type", "staff"));
    options.AddPolicy("RequireCustomer", p => p.RequireAuthenticatedUser().RequireClaim("type", "customer"));
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
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();

app.Run();
