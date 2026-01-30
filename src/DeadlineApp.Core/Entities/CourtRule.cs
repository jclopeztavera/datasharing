using DeadlineApp.Core.Enums;

namespace DeadlineApp.Core.Entities;

public class CourtRule : BaseEntity
{
    public string State { get; set; } = string.Empty;
    public string? County { get; set; }
    public string CaseType { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string RuleDescription { get; set; } = string.Empty;
    public string? RuleCitation { get; set; }
    public TriggerEventType TriggerEventType { get; set; }
    public int DaysFromTrigger { get; set; }
    public bool CountBusinessDays { get; set; }
    public bool ExcludeHolidays { get; set; } = true;
    public int? BufferDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }

    public ICollection<Deadline> Deadlines { get; set; } = new List<Deadline>();
}
