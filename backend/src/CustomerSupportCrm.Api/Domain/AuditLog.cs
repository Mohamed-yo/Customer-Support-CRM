namespace CustomerSupportCrm.Api.Domain;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // When the event happened (UTC).
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    // Discriminator: "auth.login" or "admin.role.assign" for this story.
    public string Action { get; set; } = string.Empty;

    // "success" | "failure" for login; "success" for role.assign in this story.
    public string Outcome { get; set; } = string.Empty;

    // The authenticated actor's user id when known (admin performing role.assign;
    // logged-in user on successful login). Nullable — unknown on failed login attempts.
    public Guid? ActorUserId { get; set; }

    // Human identifier of the actor when the id is not yet known — used to capture
    // the attempted email on login (both success and failure).
    public string? ActorEmail { get; set; }

    // Target user id (role.assign target; nullable for login).
    public Guid? TargetUserId { get; set; }

    // Freeform, small: role name for role.assign; error code ("invalid_credentials")
    // for failed login. Keep short — no PII beyond email above.
    public string? Details { get; set; }
}
