using DeadlineApp.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeadlineApp.Infrastructure.Data;

public class DeadlineDbContext : DbContext
{
    public DeadlineDbContext(DbContextOptions<DeadlineDbContext> options) : base(options)
    {
    }

    public DbSet<Firm> Firms => Set<Firm>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Matter> Matters => Set<Matter>();
    public DbSet<MatterAssignment> MatterAssignments => Set<MatterAssignment>();
    public DbSet<CourtRule> CourtRules => Set<CourtRule>();
    public DbSet<TriggerEvent> TriggerEvents => Set<TriggerEvent>();
    public DbSet<Deadline> Deadlines => Set<Deadline>();
    public DbSet<CalendarLink> CalendarLinks => Set<CalendarLink>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Holiday> Holidays => Set<Holiday>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Firm
        modelBuilder.Entity<Firm>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TenantId).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.TenantId).IsUnique();
        });

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntraObjectId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.EntraObjectId).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasOne(e => e.Firm)
                .WithMany(f => f.Users)
                .HasForeignKey(e => e.FirmId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Matter
        modelBuilder.Entity<Matter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MatterNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.State).HasMaxLength(50).IsRequired();
            entity.Property(e => e.County).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CaseType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RetentionTag).HasMaxLength(50);
            entity.HasIndex(e => new { e.FirmId, e.MatterNumber }).IsUnique();
            entity.HasOne(e => e.Firm)
                .WithMany(f => f.Matters)
                .HasForeignKey(e => e.FirmId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ResponsibleAttorney)
                .WithMany()
                .HasForeignKey(e => e.ResponsibleAttorneyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MatterAssignment
        modelBuilder.Entity<MatterAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MatterId, e.UserId }).IsUnique();
            entity.HasOne(e => e.Matter)
                .WithMany(m => m.Assignments)
                .HasForeignKey(e => e.MatterId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany(u => u.MatterAssignments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CourtRule
        modelBuilder.Entity<CourtRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.State).HasMaxLength(50).IsRequired();
            entity.Property(e => e.County).HasMaxLength(100);
            entity.Property(e => e.CaseType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RuleName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.RuleDescription).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.RuleCitation).HasMaxLength(200);
            entity.HasIndex(e => new { e.State, e.County, e.CaseType, e.TriggerEventType });
        });

        // TriggerEvent
        modelBuilder.Entity<TriggerEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasOne(e => e.Matter)
                .WithMany(m => m.TriggerEvents)
                .HasForeignKey(e => e.MatterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Deadline
        modelBuilder.Entity<Deadline>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.OverrideReason).HasMaxLength(1000);
            entity.Property(e => e.OverrideApprovedBy).HasMaxLength(256);
            entity.Property(e => e.CompletedBy).HasMaxLength(256);
            entity.HasIndex(e => new { e.MatterId, e.DueDate });
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.Matter)
                .WithMany(m => m.Deadlines)
                .HasForeignKey(e => e.MatterId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CourtRule)
                .WithMany(r => r.Deadlines)
                .HasForeignKey(e => e.CourtRuleId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.TriggerEvent)
                .WithMany(t => t.Deadlines)
                .HasForeignKey(e => e.TriggerEventId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // CalendarLink
        modelBuilder.Entity<CalendarLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OutlookEventId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.OutlookCalendarId).HasMaxLength(500);
            entity.Property(e => e.OutlookChangeKey).HasMaxLength(500);
            entity.Property(e => e.SyncError).HasMaxLength(2000);
            entity.HasIndex(e => new { e.DeadlineId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.OutlookEventId);
            entity.HasOne(e => e.Deadline)
                .WithOne(d => d.CalendarLink)
                .HasForeignKey<CalendarLink>(e => e.DeadlineId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AuditLog (append-only, no FK constraints for performance)
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserEmail).HasMaxLength(256);
            entity.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.MatterId);
            entity.HasIndex(e => e.UserId);
            // Make table append-only conceptually
            entity.ToTable(t => t.HasCheckConstraint("CK_AuditLog_NoUpdate", "1=1"));
        });

        // Holiday
        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.State).HasMaxLength(50);
            entity.HasIndex(e => new { e.Date, e.State });
        });
    }
}
