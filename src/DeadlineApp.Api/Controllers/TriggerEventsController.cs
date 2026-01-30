using DeadlineApp.Api.DTOs;
using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Interfaces;
using DeadlineApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeadlineApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TriggerEventsController : ControllerBase
{
    private readonly DeadlineDbContext _context;
    private readonly IMatterRepository _matterRepository;
    private readonly IDeadlineRepository _deadlineRepository;
    private readonly IDeadlineCalculator _deadlineCalculator;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserService _currentUserService;

    public TriggerEventsController(
        DeadlineDbContext context,
        IMatterRepository matterRepository,
        IDeadlineRepository deadlineRepository,
        IDeadlineCalculator deadlineCalculator,
        IAuditLogger auditLogger,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _matterRepository = matterRepository;
        _deadlineRepository = deadlineRepository;
        _deadlineCalculator = deadlineCalculator;
        _auditLogger = auditLogger;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [Authorize(Roles = "Attorney,Paralegal,Admin")]
    public async Task<ActionResult<TriggerEventResponse>> CreateTriggerEvent(
        [FromBody] CreateTriggerEventRequest request,
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

        var matter = await _matterRepository.GetByIdAsync(request.MatterId, cancellationToken);
        if (matter == null)
        {
            return NotFound("Matter not found");
        }

        var triggerEvent = new TriggerEvent
        {
            Id = Guid.NewGuid(),
            MatterId = request.MatterId,
            EventType = request.EventType,
            EventDate = request.EventDate,
            OriginalEventDate = request.EventDate,
            Description = request.Description,
            CreatedBy = _currentUserService.Email ?? "system",
            CreatedAt = DateTime.UtcNow
        };

        _context.TriggerEvents.Add(triggerEvent);
        await _context.SaveChangesAsync(cancellationToken);

        // Calculate deadlines based on this trigger event
        var deadlines = await _deadlineCalculator.CalculateDeadlinesAsync(matter, triggerEvent, cancellationToken);

        if (deadlines.Any())
        {
            await _deadlineRepository.CreateManyAsync(deadlines, cancellationToken);

            foreach (var deadline in deadlines)
            {
                await _auditLogger.LogDeadlineCreatedAsync(deadline, cancellationToken);
            }
        }

        return CreatedAtAction(nameof(GetTriggerEvent), new { id = triggerEvent.Id }, MapToTriggerEventResponse(triggerEvent));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TriggerEventResponse>> GetTriggerEvent(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var triggerEvent = await _context.TriggerEvents
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (triggerEvent == null)
        {
            return NotFound();
        }

        if (!_currentUserService.UserId.HasValue)
        {
            return Forbid();
        }

        var hasAccess = await _matterRepository.UserHasAccessAsync(
            triggerEvent.MatterId,
            _currentUserService.UserId.Value,
            cancellationToken);

        if (!hasAccess)
        {
            return Forbid();
        }

        return Ok(MapToTriggerEventResponse(triggerEvent));
    }

    [HttpGet("matter/{matterId:guid}")]
    public async Task<ActionResult<IEnumerable<TriggerEventResponse>>> GetByMatter(
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

        var events = await _context.TriggerEvents
            .Where(t => t.MatterId == matterId)
            .OrderByDescending(t => t.EventDate)
            .ToListAsync(cancellationToken);

        return Ok(events.Select(MapToTriggerEventResponse));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Attorney,Paralegal,Admin")]
    public async Task<ActionResult<TriggerEventResponse>> UpdateTriggerEvent(
        Guid id,
        [FromBody] CreateTriggerEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var triggerEvent = await _context.TriggerEvents
            .Include(t => t.Deadlines)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (triggerEvent == null)
        {
            return NotFound();
        }

        if (!_currentUserService.UserId.HasValue)
        {
            return Forbid();
        }

        var hasAccess = await _matterRepository.UserHasAccessAsync(
            triggerEvent.MatterId,
            _currentUserService.UserId.Value,
            cancellationToken);

        if (!hasAccess)
        {
            return Forbid();
        }

        var oldDate = triggerEvent.EventDate;
        triggerEvent.EventDate = request.EventDate;
        triggerEvent.Description = request.Description;
        triggerEvent.UpdatedBy = _currentUserService.Email;
        triggerEvent.UpdatedAt = DateTime.UtcNow;

        // Recalculate associated deadlines
        foreach (var deadline in triggerEvent.Deadlines.Where(d => !d.IsManualOverride))
        {
            var oldDueDate = deadline.DueDate;
            await _deadlineCalculator.RecalculateDeadlineAsync(deadline, request.EventDate, cancellationToken);
            await _auditLogger.LogDeadlineRecalculatedAsync(deadline, oldDueDate, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(MapToTriggerEventResponse(triggerEvent));
    }

    private static TriggerEventResponse MapToTriggerEventResponse(TriggerEvent triggerEvent)
    {
        return new TriggerEventResponse(
            triggerEvent.Id,
            triggerEvent.MatterId,
            triggerEvent.EventType,
            triggerEvent.EventDate,
            triggerEvent.OriginalEventDate,
            triggerEvent.Description,
            triggerEvent.CreatedAt);
    }
}
