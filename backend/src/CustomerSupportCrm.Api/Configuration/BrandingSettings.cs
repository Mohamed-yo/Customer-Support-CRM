namespace CustomerSupportCrm.Api.Configuration;

// Plain POCO shape stored as RuntimeSetting's ValueJson under RuntimeSettingKeys.Branding -
// not an EF entity, no DbSet.
public class BrandingSettings
{
    public string AppName { get; set; } = "Customer Support CRM";

    // Data URL (e.g. "data:image/png;base64,...") or null for the default/no logo.
    public string? LogoDataUrl { get; set; } = null;

    // "#rrggbb" or null for the default theme. Applied by AppShell.tsx/PortalShell.tsx as
    // the --brand-primary CSS custom property on the shell root.
    public string? PrimaryColorHex { get; set; } = null;
}
