using DeadlineApp.Core.Enums;

namespace DeadlineApp.Core.Entities;

public class User : BaseEntity
{
    public string EntraObjectId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid FirmId { get; set; }
    public Firm Firm { get; set; } = null!;

    public ICollection<MatterAssignment> MatterAssignments { get; set; } = new List<MatterAssignment>();
}
