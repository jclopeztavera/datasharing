namespace DeadlineApp.Core.Entities;

public class Firm : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Matter> Matters { get; set; } = new List<Matter>();
}
