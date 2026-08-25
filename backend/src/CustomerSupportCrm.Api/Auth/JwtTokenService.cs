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

    public string IssueToken(User user, out DateTime expiresAtUtc)
    {
        var jwt = _configuration.GetSection("Jwt");
        var signingKey = jwt["SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey missing");
        var expiryMinutes = jwt.GetValue<int?>("ExpiryMinutes") ?? 60;

        expiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("email", user.Email),
            new Claim("name", user.DisplayName),
        };

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
}
