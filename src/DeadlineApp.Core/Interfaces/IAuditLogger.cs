using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;

namespace DeadlineApp.Core.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(AuditAction action, string entityType, Guid? entityId, Guid? matterId = null, object? oldValues = null, object? newValues = null, CancellationToken cancellationToken = default);
    Task LogDeadlineCreatedAsync(Deadline deadline, CancellationToken cancellationToken = default);
    Task LogDeadlineRecalculatedAsync(Deadline deadline, DateTime oldDueDate, CancellationToken cancellationToken = default);
    Task LogDeadlineOverriddenAsync(Deadline deadline, DateTime originalDueDate, string reason, CancellationToken cancellationToken = default);
    Task LogCalendarSyncAsync(CalendarLink link, AuditAction action, string? error = null, CancellationToken cancellationToken = default);
    Task LogMatterAccessAsync(Guid matterId, CancellationToken cancellationToken = default);
}
