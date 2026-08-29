namespace CustomerSupportCrm.Api.Configuration;

public interface IBrandingService
{
    Task<BrandingSettings> GetAsync(CancellationToken ct = default);

    Task SetAsync(BrandingSettings settings, Guid? updatedByUserId, CancellationToken ct = default);
}
