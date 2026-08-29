namespace CustomerSupportCrm.Api.Ai;

// Mirrors Integrations/SendResult.cs's SendStatus shape - a transient, never-persisted
// result status is a plain C# enum in this codebase; the "no enum" rule applies only to
// DB-persisted discriminator fields (Ticket.Status, Notification.Type, ...).
public enum AiStatus
{
    Ok,
    NotConfigured,
    ProviderError,
}

public sealed record AiResult<T>(AiStatus Status, T? Value = default, string? Detail = null);
