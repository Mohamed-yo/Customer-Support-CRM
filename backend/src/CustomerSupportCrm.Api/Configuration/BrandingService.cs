namespace CustomerSupportCrm.Api.Configuration;

// Thin wrapper over IRuntimeSettings for the one "branding" key - gives BrandingController
// a typed, default-safe surface instead of every caller needing to know the raw key string.
public class BrandingService : IBrandingService
{
    private readonly IRuntimeSettings _runtimeSettings;

    public BrandingService(IRuntimeSettings runtimeSettings)
    {
        _runtimeSettings = runtimeSettings;
    }

    public Task<BrandingSettings> GetAsync(CancellationToken ct = default) =>
        _runtimeSettings.GetAsync(RuntimeSettingKeys.Branding, new BrandingSettings(), ct);

    public Task SetAsync(BrandingSettings settings, Guid? updatedByUserId, CancellationToken ct = default) =>
        _runtimeSettings.SetAsync(RuntimeSettingKeys.Branding, settings, updatedByUserId, ct);
}
