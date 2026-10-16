namespace CustomerSupportCrm.Api.Auth;

public sealed class PortalOptions
{
    // Frontend page that receives ?token=... from the verification email.
    public string VerifyEmailUrl { get; set; } = "http://localhost:5173/portal/verify-email";
}
