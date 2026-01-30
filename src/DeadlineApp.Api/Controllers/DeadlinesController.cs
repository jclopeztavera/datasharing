using DeadlineApp.Api.DTOs;
using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;
using DeadlineApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeadlineApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeadlinesController : ControllerBase
{
    private readonly IDeadlineRepository _deadlineRepository;
    private readonly IMatterRepository _matterRepository;
    private readonly IDeadlineCalculator _deadlineCalculator;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserService _currentUserService;

    public DeadlinesController(
        IDeadlineRepository deadlineRepository,
        IMatterRepository matterRepository,
        IDeadlineCalculator deadlineCalculator,
        IAuditLogger auditLogger,
        ICurrentUserService currentUserService)
    {
        _deadlineRepository = deadlineRepository;
        _matterRepository = matterRepository;
        _deadlineCalculator = deadlineCalculator;
        _auditLogger = auditLogger;
        _currentUserService = currentUserService;
    }

    [HttpGet("matter/{matterId:guid}")]
    public async Task<ActionResult<IEnumerable<DeadlineResponse>>> GetByMatter(
        Guid matterId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Forbid();
        }

        var hasAccess = await _matterRepository.UserHasAccessAsync(
            matterId,
            _currentUserService.UserId.Value,
            cancellationToken);

        if (!hasAccess)
        {
            return Forbid();
        }

        var deadlines = await _deadlineRepository.GetByMatterAsync(matterId, cancellationToken);
        return Ok(deadlines.Select(MapToDeadlineResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeadlineResponse>> GetDeadline(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var deadline = await _deadlineRepository.GetByIdAsync(id, cancellationToken);
        if (deadline == null)
        {
            return NotFound();
        }

        if (!_currentUserService.UserId.HasValue)
        {
            return Forbid();
        }

        var hasAccess = await _matterRepository.UserHasAccessAsync(
            deadline.MatterId,
            _currentUserService.UserId.Value,
            cancellationToken);

        if (!hasAccess)
        {
            return Forbid();
        }

        return Ok(MapToDeadlineResponse(deadline));
    }

    [HttpPost]
    [Authorize(Roles = "Attorney,Paralegal,Admin")]
    public async Task<ActionResult<DeadlineResponse>> CreateDeadline(
        [FromBody] CreateDeadlineRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Forbid();
        }

        var hasAccess = await _matterRepository.UserHasAccessAsync(
            request.MatterId,
            _currentUserService.UserId.Value,
            cancellationToken);

        if (!hasAccess)
        {
            return Forbid();
        }

        var deadline = new Deadline
        {
            Id = Guid.NewGuid(),
            MatterId = request.MatterId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            CalculatedDueDate = request.DueDate,
            Type = request.Type,
            Status = DeadlineStatus.Pending,
            IsManualOverride = true,
            CreatedBy = _currentUserService.Email ?? "system"
        };

        var created = await _deadlineRepository.CreateAsync(deadline, cancellationToken);
        await _auditLogger.LogDeadlineCreatedAsync(created, cancellationToken);

        return CreatedAtAction(nameof(GetDeadline), new { id = created.Id }, MapToDeadlineResponse(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Attorney,Paralegal,Admin")]
    public async Task<ActionResult<DeadlineResponse>> UpdateDeadline(
        Guid id,
        [FromBody] UpdateDeadlineRequest request,
        CancellationToken cancellationToken = default)
    {
        var deadline = await _deadlineRepository.GetByIdAsync(id, cancellationToken);
        if (deadline == null)
        {
            return NotFound();
        }

        if (!_currentUserService.UserId.HasValue)
        {
            return Forbid();
        }

        var hasAccess = await _matterRepository.UserHasAccessAsync(
            deadline.MatterId,
            _currentUserService.UserId.Value,
            cancellationToken);

        if (!hasAccess)
        {
            return Forbid();
        }

        deadline.Title = request.Title;
        deadline.Description = request.Description;
        deadline.DueDate = request.DueDate;
        deadline.Status = request.Status;
        deadline.UpdatedBy = _currentUserService.Email;

        if (request.Status == DeadlineStatus.Completed)
        {
            deadline.CompletedAt = DateTime.UtcNow;
            deadline.CompletedBy = _currentUserService.Email;
        }

        var updated = await _deadlineRepository.UpdateAsync(deadline, cancellationToken);
        return Ok(MapToDeadlineResponse(updated));
    }

    [HttpPost("{id:guid}/override")]
    [Authorize(Roles = "Attorney,Admin")]
    public async Task<ActionResult<DeadlineResponse>> OverrideDeadline(
        Guid id,
        [FromBody] OverrideDeadlineRequest request,
        CancellationToken cancellationToken = default)
    {
        var deadline = await _deadlineRepository.GetByIdAsync(id, cancellationToken);
        if (deadline == null)
        {
            return NotFound();
        }

        if (!_currentUserService.UserId.HasValue)
        {
            return Forbid();
        }

        var hasAccess = await _matterRepository.UserHasAccessAsync(
            deadline.MatterId,
            _currentUserService.UserId.Value,
            cancellationToken);

        if (!hasAccess)
        {
            return Forbid();
        }

        var originalDueDate = deadline.DueDate;

        deadline.DueDate = request.NewDueDate;
        deadline.IsManualOverride = true;
        deadline.OverrideReason = request.Reason;
        deadline.OverrideApprovedAt = DateTime.UtcNow;
        deadline.OverrideApprovedBy = _currentUserService.Email;
        deadline.UpdatedBy = _currentUserService.Email;

        var updated = await _deadlineRepository.UpdateAsync(deadline, cancellationToken);

        await _auditLogger.LogDeadlineOverriddenAsync(updated, originalDueDate, request.Reason, cancellationToken);

        return Ok(MapToDeadlineResponse(updated));
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Attorney,Paralegal,Admin")]
    public async Task<ActionResult<DeadlineResponse>> CompleteDeadline(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var deadline = await _deadlineRepository.GetByIdAsync(id, cancellationToken);
        if (deadline == null)
        {
            return NotFound();
        }

        if (!_currentUserService.UserId.HasValue)
        {
            return Forbid();
        }

        var hasAccess = await _matterRepository.UserHasAccessAsync(
            deadline.MatterId,
            _currentUserService.UserId.Value,
            cancellationToken);

        if (!hasAccess)
        {
            return Forbid();
        }

        deadline.Status = DeadlineStatus.Completed;
        deadline.CompletedAt = DateTime.UtcNow;
        deadline.CompletedBy = _currentUserService.Email;
        deadline.UpdatedBy = _currentUserService.Email;

        var updated = await _deadlineRepository.UpdateAsync(deadline, cancellationToken);

        await _auditLogger.LogAsync(
            AuditAction.DeadlineCompleted,
            nameof(Deadline),
            id,
            deadline.MatterId,
            newValues: new { deadline.CompletedAt, deadline.CompletedBy },
            cancellationToken: cancellationToken);

        return Ok(MapToDeadlineResponse(updated));
    }

    public static DeadlineResponse MapToDeadlineResponse(Deadline deadline)
    {
        return new DeadlineResponse(
            deadline.Id,
            deadline.MatterId,
            deadline.Matter?.MatterNumber ?? "",
            deadline.Matter?.Title ?? "",
            deadline.Title,
            deadline.Description,
            deadline.DueDate,
            deadline.CalculatedDueDate,
            deadline.Type,
            deadline.Status,
            deadline.IsManualOverride,
            deadline.OverrideReason,
            deadline.CourtRuleId,
            deadline.CourtRule?.RuleName,
            deadline.CreatedAt,
            deadline.UpdatedAt,
            deadline.CalendarLink != null
                ? new CalendarLinkResponse(
                    deadline.CalendarLink.Id,
                    deadline.CalendarLink.OutlookEventId,
                    deadline.CalendarLink.SyncStatus,
                    deadline.CalendarLink.LastSyncedAt,
                    deadline.CalendarLink.SyncError)
                : null);
    }
}
