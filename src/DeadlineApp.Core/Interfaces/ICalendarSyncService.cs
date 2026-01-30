using DeadlineApp.Core.Entities;

namespace DeadlineApp.Core.Interfaces;

public interface ICalendarSyncService
{
    Task<CalendarLink> SyncDeadlineToOutlookAsync(Deadline deadline, User user, CancellationToken cancellationToken = default);
    Task<bool> UpdateOutlookEventAsync(CalendarLink link, Deadline deadline, CancellationToken cancellationToken = default);
    Task<bool> DeleteOutlookEventAsync(CalendarLink link, CancellationToken cancellationToken = default);
    Task<(DateTime? lastModified, bool wasDeleted)> CheckOutlookEventStatusAsync(CalendarLink link, CancellationToken cancellationToken = default);
    Task ProcessPendingSyncsAsync(CancellationToken cancellationToken = default);
    Task DetectAndResolveConflictsAsync(CancellationToken cancellationToken = default);
}
