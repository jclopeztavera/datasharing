using DeadlineApp.Core.Enums;

namespace DeadlineApp.Api.DTOs;

public record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role,
    bool IsActive);

public record UpdateUserRoleRequest(UserRole Role);
