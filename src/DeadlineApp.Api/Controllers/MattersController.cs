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
public class MattersController : ControllerBase
{
    private readonly IMatterRepository _matterRepository;
    private readonly IDeadlineCalculator _deadlineCalculator;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserService _currentUserService;

    public MattersController(
        IMatterRepository matterRepository,
        IDeadlineCalculator deadlineCalculator,
        IAuditLogger auditLogger,
        ICurrentUserService currentUserService)
    {
        _matterRepository = matterRepository;
        _deadlineCalculator = deadlineCalculator;
        _auditLogger = auditLogger;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MatterResponse>>> GetMatters(
        [FromQuery] MatterStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.FirmId.HasValue)
        {
            return Forbid();
        }

        var matters = await _matterRepository.GetByFirmAsync(
            _currentUserService.FirmId.Value,
            status,
            cancellationToken);

        var response = matters.Select(MapToMatterResponse);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MatterDetailResponse>> GetMatter(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Forbid();
        }

        var hasAccess = await _matterRepository.UserHasAccessAsync(
            id,
            _currentUserService.UserId.Value,
            cancellationToken);

        if (!hasAccess)
        {
            return Forbid();
        }

        var matter = await _matterRepository.GetByIdWithDeadlinesAsync(id, cancellationToken);
        if (matter == null)
        {
            return NotFound();
        }

        await _auditLogger.LogMatterAccessAsync(id, cancellationToken);

        return Ok(MapToMatterDetailResponse(matter));
    }

    [HttpPost]
    [Authorize(Roles = "Attorney,Admin")]
    public async Task<ActionResult<MatterResponse>> CreateMatter(
        [FromBody] CreateMatterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.FirmId.HasValue || !_currentUserService.UserId.HasValue)
        {
            return Forbid();
        }

        var matter = new Matter
        {
            Id = Guid.NewGuid(),
            FirmId = _currentUserService.FirmId.Value,
            MatterNumber = request.MatterNumber,
            Title = request.Title,
            State = request.State,
            County = request.County,
            CaseType = request.CaseType,
            FilingDate = request.FilingDate,
            ResponsibleAttorneyId = request.ResponsibleAttorneyId,
            Status = MatterStatus.Active,
            CreatedBy = _currentUserService.Email ?? "system"
        };

        var created = await _matterRepository.CreateAsync(matter, cancellationToken);

        await _auditLogger.LogAsync(
            AuditAction.MatterCreated,
            nameof(Matter),
            created.Id,
            created.Id,
            newValues: new { matter.MatterNumber, matter.Title, matter.State, matter.County },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetMatter), new { id = created.Id }, MapToMatterResponse(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Attorney,Admin")]
    public async Task<ActionResult<MatterResponse>> UpdateMatter(
        Guid id,
        [FromBody] UpdateMatterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Forbid();
        }

        var hasAccess = await _matterRepository.UserHasAccessAsync(
            id,
            _currentUserService.UserId.Value,
            cancellationToken);

        if (!hasAccess)
        {
            return Forbid();
        }

        var matter = await _matterRepository.GetByIdAsync(id, cancellationToken);
        if (matter == null)
        {
            return NotFound();
        }

        var oldValues = new { matter.Title, matter.State, matter.County, matter.Status };

        matter.Title = request.Title;
        matter.State = request.State;
        matter.County = request.County;
        matter.CaseType = request.CaseType;
        matter.FilingDate = request.FilingDate;
        matter.ResponsibleAttorneyId = request.ResponsibleAttorneyId;
        matter.Status = request.Status;
        matter.UpdatedBy = _currentUserService.Email;

        var updated = await _matterRepository.UpdateAsync(matter, cancellationToken);

        await _auditLogger.LogAsync(
            AuditAction.MatterUpdated,
            nameof(Matter),
            id,
            id,
            oldValues: oldValues,
            newValues: new { matter.Title, matter.State, matter.County, matter.Status },
            cancellationToken: cancellationToken);

        return Ok(MapToMatterResponse(updated));
    }

    [HttpGet("my-matters")]
    public async Task<ActionResult<IEnumerable<MatterResponse>>> GetMyMatters(
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Forbid();
        }

        var matters = await _matterRepository.GetByUserAssignmentAsync(
            _currentUserService.UserId.Value,
            cancellationToken);

        return Ok(matters.Select(MapToMatterResponse));
    }

    private static MatterResponse MapToMatterResponse(Matter matter)
    {
        return new MatterResponse(
            matter.Id,
            matter.MatterNumber,
            matter.Title,
            matter.State,
            matter.County,
            matter.CaseType,
            matter.FilingDate,
            matter.Status,
            matter.ResponsibleAttorneyId,
            matter.ResponsibleAttorney?.DisplayName ?? "Unknown",
            matter.CreatedAt,
            matter.UpdatedAt);
    }

    private static MatterDetailResponse MapToMatterDetailResponse(Matter matter)
    {
        return new MatterDetailResponse(
            matter.Id,
            matter.MatterNumber,
            matter.Title,
            matter.State,
            matter.County,
            matter.CaseType,
            matter.FilingDate,
            matter.Status,
            matter.ResponsibleAttorneyId,
            matter.ResponsibleAttorney?.DisplayName ?? "Unknown",
            matter.IsOnLegalHold,
            matter.RetentionTag,
            matter.CreatedAt,
            matter.UpdatedAt,
            matter.Deadlines.Select(DeadlinesController.MapToDeadlineResponse),
            matter.Assignments.Select(a => new UserAssignmentResponse(
                a.UserId,
                a.User?.DisplayName ?? "Unknown",
                a.User?.Email ?? "",
                a.AssignedRole)));
    }
}
