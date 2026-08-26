using CustomerSupportCrm.Api.Auth;
using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<PasswordHasher<User>>();
builder.Services.AddSingleton<JwtTokenService>();

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
    });
builder.Services.AddAuthorization();

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

app.Run();
