using DeadlineApp.Core.Enums;

namespace DeadlineApp.Api.DTOs;

public record CreateMatterRequest(
    string MatterNumber,
    string Title,
    string State,
    string County,
    string CaseType,
    DateTime FilingDate,
    Guid ResponsibleAttorneyId);

public record UpdateMatterRequest(
    string Title,
    string State,
    string County,
    string CaseType,
    DateTime FilingDate,
    Guid ResponsibleAttorneyId,
    MatterStatus Status);

public record MatterResponse(
    Guid Id,
    string MatterNumber,
    string Title,
    string State,
    string County,
    string CaseType,
    DateTime FilingDate,
    MatterStatus Status,
    Guid ResponsibleAttorneyId,
    string ResponsibleAttorneyName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record MatterDetailResponse(
    Guid Id,
    string MatterNumber,
    string Title,
    string State,
    string County,
    string CaseType,
    DateTime FilingDate,
    MatterStatus Status,
    Guid ResponsibleAttorneyId,
    string ResponsibleAttorneyName,
    bool IsOnLegalHold,
    string? RetentionTag,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IEnumerable<DeadlineResponse> Deadlines,
    IEnumerable<UserAssignmentResponse> Assignments);

public record UserAssignmentResponse(
    Guid UserId,
    string UserName,
    string Email,
    UserRole Role);
