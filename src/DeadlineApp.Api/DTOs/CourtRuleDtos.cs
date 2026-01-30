using DeadlineApp.Core.Enums;

namespace DeadlineApp.Api.DTOs;

public record CreateCourtRuleRequest(
    string State,
    string? County,
    string CaseType,
    string RuleName,
    string RuleDescription,
    string? RuleCitation,
    TriggerEventType TriggerEventType,
    int DaysFromTrigger,
    bool CountBusinessDays,
    bool ExcludeHolidays,
    int? BufferDays,
    DateTime EffectiveDate,
    DateTime? ExpirationDate);

public record UpdateCourtRuleRequest(
    string RuleName,
    string RuleDescription,
    string? RuleCitation,
    int DaysFromTrigger,
    bool CountBusinessDays,
    bool ExcludeHolidays,
    int? BufferDays,
    bool IsActive,
    DateTime? ExpirationDate);

public record CourtRuleResponse(
    Guid Id,
    string State,
    string? County,
    string CaseType,
    string RuleName,
    string RuleDescription,
    string? RuleCitation,
    TriggerEventType TriggerEventType,
    int DaysFromTrigger,
    bool CountBusinessDays,
    bool ExcludeHolidays,
    int? BufferDays,
    bool IsActive,
    DateTime EffectiveDate,
    DateTime? ExpirationDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
