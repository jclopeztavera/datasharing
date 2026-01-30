namespace DeadlineApp.Core.Entities;

public class Holiday : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? State { get; set; }
    public bool IsFederal { get; set; }
    public bool IsCourtClosed { get; set; } = true;
}
