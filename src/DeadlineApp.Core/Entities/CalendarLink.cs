using DeadlineApp.Core.Enums;

namespace DeadlineApp.Core.Entities;

public class CalendarLink : BaseEntity
{
    public Guid DeadlineId { get; set; }
    public Deadline Deadline { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string OutlookEventId { get; set; } = string.Empty;
    public string? OutlookCalendarId { get; set; }
    public string? OutlookChangeKey { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;
    public string? SyncError { get; set; }
    public DateTime? OutlookLastModified { get; set; }
    public DateTime? AppLastModified { get; set; }
}
