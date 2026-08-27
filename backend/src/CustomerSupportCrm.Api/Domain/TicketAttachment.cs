namespace CustomerSupportCrm.Api.Domain;

// File content stored inline (varbinary(max)) — no external blob storage is
// configured anywhere in this project, and adding one is out of scope here.
public class TicketAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public Guid UploadedByUserId { get; set; }
    public User? UploadedByUser { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
