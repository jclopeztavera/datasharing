using DeadlineApp.Core.Enums;

namespace DeadlineApp.Core.Entities;

public class Deadline : BaseEntity
{
    public Guid MatterId { get; set; }
    public Matter Matter { get; set; } = null!;

    public Guid? CourtRuleId { get; set; }
    public CourtRule? CourtRule { get; set; }

    public Guid? TriggerEventId { get; set; }
    public TriggerEvent? TriggerEvent { get; set; }

    public DeadlineType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CalculatedDueDate { get; set; }
    public bool IsManualOverride { get; set; }
    public string? OverrideReason { get; set; }
    public DateTime? OverrideApprovedAt { get; set; }
    public string? OverrideApprovedBy { get; set; }
    public DeadlineStatus Status { get; set; } = DeadlineStatus.Pending;
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }

    public CalendarLink? CalendarLink { get; set; }
}
