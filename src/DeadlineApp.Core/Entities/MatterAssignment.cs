using DeadlineApp.Core.Enums;

namespace DeadlineApp.Core.Entities;

public class MatterAssignment : BaseEntity
{
    public Guid MatterId { get; set; }
    public Matter Matter { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public UserRole AssignedRole { get; set; }
    public bool IsActive { get; set; } = true;
}
