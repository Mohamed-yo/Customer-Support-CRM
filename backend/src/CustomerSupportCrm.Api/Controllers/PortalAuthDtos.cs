namespace CustomerSupportCrm.Api.Controllers;

public record CustomerRegisterRequest(string FullName, string Email, string? Phone, string Password);
public record CustomerLoginRequest(string Email, string Password);
public record CustomerLoginResponse(Guid CustomerId, string Token, string Email, string FullName, DateTime ExpiresAtUtc);
public record CustomerMeResponse(Guid CustomerId, string Email, string FullName);
