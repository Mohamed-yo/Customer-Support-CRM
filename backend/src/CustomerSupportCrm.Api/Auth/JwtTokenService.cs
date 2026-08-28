using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CustomerSupportCrm.Api.Domain;
using Microsoft.IdentityModel.Tokens;

namespace CustomerSupportCrm.Api.Auth;

public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // "staff" | "customer" - the claim ASP.NET Core authorization policies key off
    // (see Program.cs "RequireStaff"/"RequireCustomer") to keep the two identity kinds
    // strictly separated on a single JWT bearer scheme.
    public string IssueToken(
        Guid subjectId,
        string email,
        string displayName,
        string identityType,
        IEnumerable<string> roles,
        out DateTime expiresAtUtc)
    {
        var jwt = _configuration.GetSection("Jwt");
        var signingKey = jwt["SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey missing");
        var expiryMinutes = jwt.GetValue<int?>("ExpiryMinutes") ?? 60;

        expiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subjectId.ToString()),
            new("email", email),
            new("name", displayName),
            new("type", identityType),
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string IssueToken(User user, IEnumerable<string> roles, out DateTime expiresAtUtc) =>
        IssueToken(user.Id, user.Email, user.DisplayName, "staff", roles, out expiresAtUtc);
}
