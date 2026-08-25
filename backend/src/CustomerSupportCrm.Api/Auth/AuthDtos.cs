namespace CustomerSupportCrm.Api.Auth;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, string Email, string DisplayName, DateTime ExpiresAtUtc);
public record MeResponse(Guid Id, string Email, string DisplayName);
