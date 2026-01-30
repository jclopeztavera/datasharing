using DeadlineApp.Core.Enums;

namespace DeadlineApp.Core.Entities;

public class Matter : BaseEntity
{
    public string MatterNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string CaseType { get; set; } = string.Empty;
    public DateTime FilingDate { get; set; }
    public MatterStatus Status { get; set; } = MatterStatus.Active;
    public string? RetentionTag { get; set; }
    public bool IsOnLegalHold { get; set; }

    public Guid FirmId { get; set; }
    public Firm Firm { get; set; } = null!;

    public Guid ResponsibleAttorneyId { get; set; }
    public User ResponsibleAttorney { get; set; } = null!;

    public ICollection<MatterAssignment> Assignments { get; set; } = new List<MatterAssignment>();
    public ICollection<Deadline> Deadlines { get; set; } = new List<Deadline>();
    public ICollection<TriggerEvent> TriggerEvents { get; set; } = new List<TriggerEvent>();
}
