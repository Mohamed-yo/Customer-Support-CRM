namespace CustomerSupportCrm.Api.Configuration;

// Admin-editable key/value config, backed by the RuntimeSetting table. Every value is opaque
// JSON from this interface's point of view - callers supply the shape via T and a fallback
// used both when no row exists yet and (defensively) if the stored JSON fails to deserialize.
public interface IRuntimeSettings
{
    Task<T> GetAsync<T>(string key, T fallback, CancellationToken ct = default);

    Task SetAsync<T>(string key, T value, Guid? updatedByUserId, CancellationToken ct = default);
}
