using System.Text.Json;
using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;
using DeadlineApp.Core.Interfaces;
using DeadlineApp.Infrastructure.Data;

namespace DeadlineApp.Infrastructure.Services;

public class AuditLogger : IAuditLogger
{
    private readonly DeadlineDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AuditLogger(DeadlineDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task LogAsync(
        AuditAction action,
        string entityType,
        Guid? entityId,
        Guid? matterId = null,
        object? oldValues = null,
        object? newValues = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            UserId = _currentUserService.UserId,
            UserEmail = _currentUserService.Email,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            MatterId = matterId,
            OldValues = oldValues != null ? SerializeForAudit(oldValues) : null,
            NewValues = newValues != null ? SerializeForAudit(newValues) : null,
            IpAddress = _currentUserService.IpAddress,
            UserAgent = _currentUserService.UserAgent,
            IsSuccess = true
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task LogDeadlineCreatedAsync(Deadline deadline, CancellationToken cancellationToken = default)
    {
        await LogAsync(
            AuditAction.DeadlineCreated,
            nameof(Deadline),
            deadline.Id,
            deadline.MatterId,
            newValues: new
            {
                deadline.Title,
                deadline.DueDate,
                deadline.Type,
                CourtRuleId = deadline.CourtRuleId,
                TriggerEventId = deadline.TriggerEventId
            },
            cancellationToken: cancellationToken);
    }

    public async Task LogDeadlineRecalculatedAsync(Deadline deadline, DateTime oldDueDate, CancellationToken cancellationToken = default)
    {
        await LogAsync(
            AuditAction.DeadlineRecalculated,
            nameof(Deadline),
            deadline.Id,
            deadline.MatterId,
            oldValues: new { DueDate = oldDueDate },
            newValues: new { deadline.DueDate, deadline.CalculatedDueDate },
            cancellationToken);
    }

    public async Task LogDeadlineOverriddenAsync(Deadline deadline, DateTime originalDueDate, string reason, CancellationToken cancellationToken = default)
    {
        await LogAsync(
            AuditAction.DeadlineOverridden,
            nameof(Deadline),
            deadline.Id,
            deadline.MatterId,
            oldValues: new { DueDate = originalDueDate },
            newValues: new { deadline.DueDate, OverrideReason = reason, deadline.OverrideApprovedBy },
            cancellationToken);
    }

    public async Task LogCalendarSyncAsync(CalendarLink link, AuditAction action, string? error = null, CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            UserId = _currentUserService.UserId,
            UserEmail = _currentUserService.Email,
            Action = action,
            EntityType = nameof(CalendarLink),
            EntityId = link.Id,
            MatterId = link.Deadline?.MatterId,
            NewValues = SerializeForAudit(new
            {
                link.OutlookEventId,
                link.SyncStatus,
                link.LastSyncedAt
            }),
            IpAddress = _currentUserService.IpAddress,
            IsSuccess = string.IsNullOrEmpty(error),
            ErrorMessage = error
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task LogMatterAccessAsync(Guid matterId, CancellationToken cancellationToken = default)
    {
        await LogAsync(
            AuditAction.MatterAccessed,
            nameof(Matter),
            matterId,
            matterId,
            cancellationToken: cancellationToken);
    }

    private static string SerializeForAudit(object obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
