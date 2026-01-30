using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;
using DeadlineApp.Core.Interfaces;
using DeadlineApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace DeadlineApp.Infrastructure.Services;

public class CalendarSyncService : ICalendarSyncService
{
    private readonly DeadlineDbContext _context;
    private readonly IAuditLogger _auditLogger;
    private readonly GraphServiceClient _graphClient;

    public CalendarSyncService(
        DeadlineDbContext context,
        IAuditLogger auditLogger,
        GraphServiceClient graphClient)
    {
        _context = context;
        _auditLogger = auditLogger;
        _graphClient = graphClient;
    }

    public async Task<CalendarLink> SyncDeadlineToOutlookAsync(
        Deadline deadline,
        User user,
        CancellationToken cancellationToken = default)
    {
        var calendarEvent = CreateCalendarEvent(deadline);

        try
        {
            var createdEvent = await _graphClient.Users[user.EntraObjectId]
                .Calendar
                .Events
                .PostAsync(calendarEvent, cancellationToken: cancellationToken);

            var calendarLink = new CalendarLink
            {
                Id = Guid.NewGuid(),
                DeadlineId = deadline.Id,
                UserId = user.Id,
                OutlookEventId = createdEvent!.Id!,
                OutlookChangeKey = createdEvent.ChangeKey,
                LastSyncedAt = DateTime.UtcNow,
                SyncStatus = SyncStatus.Synced,
                AppLastModified = deadline.UpdatedAt ?? deadline.CreatedAt,
                OutlookLastModified = createdEvent.LastModifiedDateTime?.UtcDateTime,
                CreatedAt = DateTime.UtcNow
            };

            _context.CalendarLinks.Add(calendarLink);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogCalendarSyncAsync(calendarLink, AuditAction.CalendarEventCreated, cancellationToken: cancellationToken);

            return calendarLink;
        }
        catch (Exception ex)
        {
            var failedLink = new CalendarLink
            {
                Id = Guid.NewGuid(),
                DeadlineId = deadline.Id,
                UserId = user.Id,
                OutlookEventId = string.Empty,
                LastSyncedAt = DateTime.UtcNow,
                SyncStatus = SyncStatus.Failed,
                SyncError = ex.Message,
                CreatedAt = DateTime.UtcNow
            };

            _context.CalendarLinks.Add(failedLink);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogCalendarSyncAsync(failedLink, AuditAction.CalendarSyncFailed, ex.Message, cancellationToken);

            throw;
        }
    }

    public async Task<bool> UpdateOutlookEventAsync(
        CalendarLink link,
        Deadline deadline,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(link.OutlookEventId))
        {
            return false;
        }

        var calendarEvent = CreateCalendarEvent(deadline);

        try
        {
            var user = await _context.Users.FindAsync(new object[] { link.UserId }, cancellationToken);
            if (user == null) return false;

            var updatedEvent = await _graphClient.Users[user.EntraObjectId]
                .Calendar
                .Events[link.OutlookEventId]
                .PatchAsync(calendarEvent, cancellationToken: cancellationToken);

            link.OutlookChangeKey = updatedEvent!.ChangeKey;
            link.LastSyncedAt = DateTime.UtcNow;
            link.SyncStatus = SyncStatus.Synced;
            link.SyncError = null;
            link.AppLastModified = deadline.UpdatedAt ?? deadline.CreatedAt;
            link.OutlookLastModified = updatedEvent.LastModifiedDateTime?.UtcDateTime;
            link.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogCalendarSyncAsync(link, AuditAction.CalendarEventUpdated, cancellationToken: cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            link.SyncStatus = SyncStatus.Failed;
            link.SyncError = ex.Message;
            link.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogCalendarSyncAsync(link, AuditAction.CalendarSyncFailed, ex.Message, cancellationToken);

            return false;
        }
    }

    public async Task<bool> DeleteOutlookEventAsync(
        CalendarLink link,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(link.OutlookEventId))
        {
            return false;
        }

        try
        {
            var user = await _context.Users.FindAsync(new object[] { link.UserId }, cancellationToken);
            if (user == null) return false;

            await _graphClient.Users[user.EntraObjectId]
                .Calendar
                .Events[link.OutlookEventId]
                .DeleteAsync(cancellationToken: cancellationToken);

            _context.CalendarLinks.Remove(link);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogCalendarSyncAsync(link, AuditAction.CalendarEventDeleted, cancellationToken: cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            link.SyncStatus = SyncStatus.Failed;
            link.SyncError = ex.Message;

            await _context.SaveChangesAsync(cancellationToken);

            return false;
        }
    }

    public async Task<(DateTime? lastModified, bool wasDeleted)> CheckOutlookEventStatusAsync(
        CalendarLink link,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(link.OutlookEventId))
        {
            return (null, true);
        }

        try
        {
            var user = await _context.Users.FindAsync(new object[] { link.UserId }, cancellationToken);
            if (user == null) return (null, false);

            var calendarEvent = await _graphClient.Users[user.EntraObjectId]
                .Calendar
                .Events[link.OutlookEventId]
                .GetAsync(cancellationToken: cancellationToken);

            return (calendarEvent?.LastModifiedDateTime?.UtcDateTime, false);
        }
        catch (ServiceException ex) when (ex.ResponseStatusCode == 404)
        {
            return (null, true);
        }
    }

    public async Task ProcessPendingSyncsAsync(CancellationToken cancellationToken = default)
    {
        var pendingLinks = await _context.CalendarLinks
            .Include(l => l.Deadline)
            .Include(l => l.User)
            .Where(l => l.SyncStatus == SyncStatus.Pending || l.SyncStatus == SyncStatus.Failed)
            .ToListAsync(cancellationToken);

        foreach (var link in pendingLinks)
        {
            if (link.Deadline != null && link.User != null)
            {
                await UpdateOutlookEventAsync(link, link.Deadline, cancellationToken);
            }
        }
    }

    public async Task DetectAndResolveConflictsAsync(CancellationToken cancellationToken = default)
    {
        var syncedLinks = await _context.CalendarLinks
            .Include(l => l.Deadline)
            .Include(l => l.User)
            .Where(l => l.SyncStatus == SyncStatus.Synced && !string.IsNullOrEmpty(l.OutlookEventId))
            .ToListAsync(cancellationToken);

        foreach (var link in syncedLinks)
        {
            var (outlookLastModified, wasDeleted) = await CheckOutlookEventStatusAsync(link, cancellationToken);

            if (wasDeleted)
            {
                link.SyncStatus = SyncStatus.Conflict;
                link.SyncError = "Event was deleted in Outlook";
                continue;
            }

            if (outlookLastModified.HasValue &&
                link.OutlookLastModified.HasValue &&
                outlookLastModified > link.OutlookLastModified)
            {
                // Outlook was modified more recently - mark as conflict
                // Source of truth: App wins, but we flag for review
                link.SyncStatus = SyncStatus.Conflict;
                link.SyncError = "Event was modified in Outlook after last sync";
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static Event CreateCalendarEvent(Deadline deadline)
    {
        var deadlineDate = deadline.DueDate.Date;

        return new Event
        {
            Subject = $"[{(deadline.Type == DeadlineType.Court ? "COURT" : "BUFFER")}] {deadline.Title}",
            Body = new ItemBody
            {
                ContentType = BodyType.Text,
                Content = deadline.Description ?? $"Deadline for matter. Due: {deadline.DueDate:d}"
            },
            Start = new DateTimeTimeZone
            {
                DateTime = deadlineDate.ToString("yyyy-MM-ddT09:00:00"),
                TimeZone = "Eastern Standard Time"
            },
            End = new DateTimeTimeZone
            {
                DateTime = deadlineDate.ToString("yyyy-MM-ddT09:30:00"),
                TimeZone = "Eastern Standard Time"
            },
            IsAllDay = false,
            IsReminderOn = true,
            ReminderMinutesBeforeStart = 1440, // 24 hours
            ShowAs = FreeBusyStatus.Busy,
            Categories = new List<string>
            {
                deadline.Type == DeadlineType.Court ? "Court Deadline" : "Internal Buffer"
            }
        };
    }
}
