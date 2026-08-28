using CustomerSupportCrm.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupportCrm.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketNote> TicketNotes => Set<TicketNote>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketTask> TicketTasks => Set<TicketTask>();
    public DbSet<QuickReplyTemplate> QuickReplyTemplates => Set<QuickReplyTemplate>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);
            e.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Name).IsRequired().HasMaxLength(64);
            e.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(ur => new { ur.UserId, ur.RoleId });
            e.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Action).IsRequired().HasMaxLength(64);
            e.Property(a => a.Outcome).IsRequired().HasMaxLength(64);
            e.Property(a => a.ActorEmail).HasMaxLength(256);
            e.Property(a => a.Details).HasMaxLength(512);
            e.HasIndex(a => a.TimestampUtc);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.FullName).IsRequired().HasMaxLength(200);
            e.Property(c => c.Email).IsRequired().HasMaxLength(256);
            e.Property(c => c.Phone).HasMaxLength(64);
            e.HasIndex(c => c.Email); // non-unique: same email may appear on distinct customer records (no dedup this story)
            e.HasIndex(c => c.CreatedAtUtc);
        });

        modelBuilder.Entity<Ticket>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Subject).IsRequired().HasMaxLength(200);
            e.Property(t => t.Description).HasMaxLength(4000);
            e.Property(t => t.Status).IsRequired().HasMaxLength(20);
            e.Property(t => t.Category).IsRequired().HasMaxLength(32);
            e.Property(t => t.Priority).IsRequired().HasMaxLength(16);
            e.HasIndex(t => t.CreatedAtUtc);
            // Restrict (not Cascade): a customer with existing tickets cannot be silently
            // orphaned by deleting the customer. Deleting such a customer fails loudly
            // until a follow-up story addresses cascading UX for dependent tickets.
            e.HasOne(t => t.Customer)
                .WithMany()
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            // SetNull (not Restrict): unlike Customer, an agent leaving the system should
            // not block ticket updates. The ticket falls back to the valid "Unassigned" state.
            e.HasOne(t => t.AssignedToUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TicketNote>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Body).IsRequired().HasMaxLength(4000);
            e.HasIndex(n => n.TicketId);
            e.HasOne(n => n.Ticket)
                .WithMany()
                .HasForeignKey(n => n.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(n => n.AuthorUser)
                .WithMany()
                .HasForeignKey(n => n.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TicketAttachment>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.FileName).IsRequired().HasMaxLength(260);
            e.Property(a => a.ContentType).IsRequired().HasMaxLength(200);
            e.Property(a => a.Content).HasColumnType("varbinary(max)");
            e.HasIndex(a => a.TicketId);
            e.HasOne(a => a.Ticket)
                .WithMany()
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.UploadedByUser)
                .WithMany()
                .HasForeignKey(a => a.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TicketTask>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).IsRequired().HasMaxLength(400);
            e.HasIndex(t => t.TicketId);
            e.HasOne(t => t.Ticket)
                .WithMany()
                .HasForeignKey(t => t.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuickReplyTemplate>(e =>
        {
            e.HasKey(q => q.Id);
            e.Property(q => q.Title).IsRequired().HasMaxLength(200);
            e.Property(q => q.Body).IsRequired().HasMaxLength(4000);
            e.HasOne(q => q.CreatedByUser)
                .WithMany()
                .HasForeignKey(q => q.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Type).IsRequired().HasMaxLength(32);
            e.Property(n => n.Message).IsRequired().HasMaxLength(512);
            e.HasIndex(n => new { n.UserId, n.IsRead });
            e.HasIndex(n => new { n.TicketId, n.Type, n.IsRead });
            e.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            // Cascade: notifications about a ticket are meaningless once the ticket is gone.
            e.HasOne(n => n.Ticket)
                .WithMany()
                .HasForeignKey(n => n.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
