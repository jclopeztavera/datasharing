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
public class CourtRulesController : ControllerBase
{
    private readonly ICourtRuleRepository _courtRuleRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserService _currentUserService;

    public CourtRulesController(
        ICourtRuleRepository courtRuleRepository,
        IAuditLogger auditLogger,
        ICurrentUserService currentUserService)
    {
        _courtRuleRepository = courtRuleRepository;
        _auditLogger = auditLogger;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourtRuleResponse>>> GetRules(
        [FromQuery] string state,
        [FromQuery] string? county = null,
        [FromQuery] string? caseType = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(caseType))
        {
            caseType = "Divorce"; // Default case type for family law
        }

        var rules = await _courtRuleRepository.GetRulesForMatterAsync(state, county, caseType, cancellationToken);
        return Ok(rules.Select(MapToCourtRuleResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CourtRuleResponse>> GetRule(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var rule = await _courtRuleRepository.GetByIdAsync(id, cancellationToken);
        if (rule == null)
        {
            return NotFound();
        }

        return Ok(MapToCourtRuleResponse(rule));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CourtRuleResponse>> CreateRule(
        [FromBody] CreateCourtRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var rule = new CourtRule
        {
            Id = Guid.NewGuid(),
            State = request.State,
            County = request.County,
            CaseType = request.CaseType,
            RuleName = request.RuleName,
            RuleDescription = request.RuleDescription,
            RuleCitation = request.RuleCitation,
            TriggerEventType = request.TriggerEventType,
            DaysFromTrigger = request.DaysFromTrigger,
            CountBusinessDays = request.CountBusinessDays,
            ExcludeHolidays = request.ExcludeHolidays,
            BufferDays = request.BufferDays,
            EffectiveDate = request.EffectiveDate,
            ExpirationDate = request.ExpirationDate,
            IsActive = true,
            CreatedBy = _currentUserService.Email ?? "system"
        };

        var created = await _courtRuleRepository.CreateAsync(rule, cancellationToken);

        await _auditLogger.LogAsync(
            AuditAction.CourtRuleCreated,
            nameof(CourtRule),
            created.Id,
            newValues: new { rule.State, rule.County, rule.CaseType, rule.RuleName },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetRule), new { id = created.Id }, MapToCourtRuleResponse(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CourtRuleResponse>> UpdateRule(
        Guid id,
        [FromBody] UpdateCourtRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var rule = await _courtRuleRepository.GetByIdAsync(id, cancellationToken);
        if (rule == null)
        {
            return NotFound();
        }

        var oldValues = new { rule.RuleName, rule.DaysFromTrigger, rule.BufferDays, rule.IsActive };

        rule.RuleName = request.RuleName;
        rule.RuleDescription = request.RuleDescription;
        rule.RuleCitation = request.RuleCitation;
        rule.DaysFromTrigger = request.DaysFromTrigger;
        rule.CountBusinessDays = request.CountBusinessDays;
        rule.ExcludeHolidays = request.ExcludeHolidays;
        rule.BufferDays = request.BufferDays;
        rule.IsActive = request.IsActive;
        rule.ExpirationDate = request.ExpirationDate;
        rule.UpdatedBy = _currentUserService.Email;

        var updated = await _courtRuleRepository.UpdateAsync(rule, cancellationToken);

        await _auditLogger.LogAsync(
            AuditAction.CourtRuleUpdated,
            nameof(CourtRule),
            id,
            oldValues: oldValues,
            newValues: new { rule.RuleName, rule.DaysFromTrigger, rule.BufferDays, rule.IsActive },
            cancellationToken: cancellationToken);

        return Ok(MapToCourtRuleResponse(updated));
    }

    private static CourtRuleResponse MapToCourtRuleResponse(CourtRule rule)
    {
        return new CourtRuleResponse(
            rule.Id,
            rule.State,
            rule.County,
            rule.CaseType,
            rule.RuleName,
            rule.RuleDescription,
            rule.RuleCitation,
            rule.TriggerEventType,
            rule.DaysFromTrigger,
            rule.CountBusinessDays,
            rule.ExcludeHolidays,
            rule.BufferDays,
            rule.IsActive,
            rule.EffectiveDate,
            rule.ExpirationDate,
            rule.CreatedAt,
            rule.UpdatedAt);
    }
}
