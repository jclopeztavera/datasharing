using DeadlineApp.Api.DTOs;
using DeadlineApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeadlineApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDeadlineRepository _deadlineRepository;
    private readonly IMatterRepository _matterRepository;
    private readonly ICurrentUserService _currentUserService;

    public DashboardController(
        IDeadlineRepository deadlineRepository,
        IMatterRepository matterRepository,
        ICurrentUserService currentUserService)
    {
        _deadlineRepository = deadlineRepository;
        _matterRepository = matterRepository;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> GetDashboard(
        [FromQuery] int upcomingDays = 30,
        [FromQuery] int atRiskThreshold = 3,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.FirmId.HasValue)
        {
            return Forbid();
        }

        var firmId = _currentUserService.FirmId.Value;

        var upcomingTask = _deadlineRepository.GetUpcomingAsync(firmId, upcomingDays, cancellationToken);
        var overdueTask = _deadlineRepository.GetOverdueAsync(firmId, cancellationToken);
        var atRiskTask = _deadlineRepository.GetAtRiskBufferDeadlinesAsync(firmId, atRiskThreshold, cancellationToken);
        var mattersTask = _matterRepository.GetByFirmAsync(firmId, Core.Enums.MatterStatus.Active, cancellationToken);

        await Task.WhenAll(upcomingTask, overdueTask, atRiskTask, mattersTask);

        var upcoming = await upcomingTask;
        var overdue = await overdueTask;
        var atRisk = await atRiskTask;
        var matters = await mattersTask;

        var response = new DashboardResponse(
            upcoming.Select(DeadlinesController.MapToDeadlineResponse),
            overdue.Select(DeadlinesController.MapToDeadlineResponse),
            atRisk.Select(DeadlinesController.MapToDeadlineResponse),
            new DashboardSummary(
                upcoming.Count(),
                overdue.Count(),
                atRisk.Count(),
                matters.Count()));

        return Ok(response);
    }

    [HttpGet("by-matter")]
    public async Task<ActionResult<IEnumerable<MatterDeadlineGroup>>> GetDeadlinesGroupedByMatter(
        [FromQuery] int upcomingDays = 30,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.FirmId.HasValue)
        {
            return Forbid();
        }

        var firmId = _currentUserService.FirmId.Value;
        var deadlines = await _deadlineRepository.GetUpcomingAsync(firmId, upcomingDays, cancellationToken);

        var grouped = deadlines
            .GroupBy(d => d.MatterId)
            .Select(g => new MatterDeadlineGroup(
                g.Key,
                g.First().Matter?.MatterNumber ?? "",
                g.First().Matter?.Title ?? "",
                g.Select(DeadlinesController.MapToDeadlineResponse)));

        return Ok(grouped);
    }

    [HttpGet("my-deadlines")]
    public async Task<ActionResult<IEnumerable<DeadlineResponse>>> GetMyDeadlines(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Forbid();
        }

        var from = fromDate ?? DateTime.UtcNow.Date;
        var to = toDate ?? DateTime.UtcNow.Date.AddDays(30);

        var deadlines = await _deadlineRepository.GetByResponsibleAttorneyAsync(
            _currentUserService.UserId.Value,
            from,
            to,
            cancellationToken);

        return Ok(deadlines.Select(DeadlinesController.MapToDeadlineResponse));
    }
}
