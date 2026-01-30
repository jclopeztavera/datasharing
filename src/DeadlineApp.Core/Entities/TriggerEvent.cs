using DeadlineApp.Core.Enums;

namespace DeadlineApp.Core.Entities;

public class TriggerEvent : BaseEntity
{
    public Guid MatterId { get; set; }
    public Matter Matter { get; set; } = null!;

    public TriggerEventType EventType { get; set; }
    public DateTime EventDate { get; set; }
    public DateTime OriginalEventDate { get; set; }
    public string? Description { get; set; }

    public ICollection<Deadline> Deadlines { get; set; } = new List<Deadline>();
}
