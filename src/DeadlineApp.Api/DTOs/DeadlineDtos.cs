using DeadlineApp.Core.Enums;

namespace DeadlineApp.Api.DTOs;

public record CreateDeadlineRequest(
    Guid MatterId,
    string Title,
    string? Description,
    DateTime DueDate,
    DeadlineType Type);

public record UpdateDeadlineRequest(
    string Title,
    string? Description,
    DateTime DueDate,
    DeadlineStatus Status);

public record OverrideDeadlineRequest(
    DateTime NewDueDate,
    string Reason);

public record DeadlineResponse(
    Guid Id,
    Guid MatterId,
    string MatterNumber,
    string MatterTitle,
    string Title,
    string? Description,
    DateTime DueDate,
    DateTime CalculatedDueDate,
    DeadlineType Type,
    DeadlineStatus Status,
    bool IsManualOverride,
    string? OverrideReason,
    Guid? CourtRuleId,
    string? CourtRuleName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    CalendarLinkResponse? CalendarLink);

public record CalendarLinkResponse(
    Guid Id,
    string OutlookEventId,
    SyncStatus SyncStatus,
    DateTime LastSyncedAt,
    string? SyncError);

public record CreateTriggerEventRequest(
    Guid MatterId,
    TriggerEventType EventType,
    DateTime EventDate,
    string? Description);

public record TriggerEventResponse(
    Guid Id,
    Guid MatterId,
    TriggerEventType EventType,
    DateTime EventDate,
    DateTime OriginalEventDate,
    string? Description,
    DateTime CreatedAt);
