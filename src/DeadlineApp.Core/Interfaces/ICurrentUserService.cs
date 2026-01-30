namespace DeadlineApp.Core.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? FirmId { get; }
    string? Email { get; }
    string? EntraObjectId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
    bool IsInRole(string role);
}
