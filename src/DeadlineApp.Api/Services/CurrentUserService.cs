using System.Security.Claims;
using DeadlineApp.Core.Interfaces;

namespace DeadlineApp.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var userIdClaim = User?.FindFirst("app_user_id")?.Value;
            return Guid.TryParse(userIdClaim, out var id) ? id : null;
        }
    }

    public Guid? FirmId
    {
        get
        {
            var firmIdClaim = User?.FindFirst("firm_id")?.Value;
            return Guid.TryParse(firmIdClaim, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value
                            ?? User?.FindFirst("preferred_username")?.Value;

    public string? EntraObjectId => User?.FindFirst("oid")?.Value
                                    ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<string> Roles => User?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? Enumerable.Empty<string>();

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}
