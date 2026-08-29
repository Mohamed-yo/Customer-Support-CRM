namespace CustomerSupportCrm.Api.Domain;

// Generic admin-configurable key/value store (SLA targets, reminder lead time, branding, ...).
// Key is the primary key - one row per logical setting, value is opaque serialized JSON.
public class RuntimeSetting
{
    public string Key { get; set; } = string.Empty;

    public string ValueJson { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // No FK - mirrors AuditLog.ActorUserId's generic actor id precedent.
    public Guid? UpdatedByUserId { get; set; }
}
