using CustomerSupportCrm.Api.Data;
using CustomerSupportCrm.Api.Domain;

namespace CustomerSupportCrm.Api.Auditing;

public class AuditLogger
{
    private readonly AppDbContext _db;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(AppDbContext db, ILogger<AuditLogger> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task WriteAsync(AuditLog entry, CancellationToken ct = default)
    {
        try
        {
            _db.AuditLogs.Add(entry);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Mirrors Program.cs's dev-seed try/catch: audit failures must never
            // break the audited operation. Log a warning and swallow.
            _logger.LogWarning(ex, "Audit write failed for action {Action}", entry.Action);
        }
    }
}
