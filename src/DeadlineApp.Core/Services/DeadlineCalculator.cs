using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;
using DeadlineApp.Core.Interfaces;

namespace DeadlineApp.Core.Services;

public class DeadlineCalculator : IDeadlineCalculator
{
    private readonly ICourtRuleRepository _courtRuleRepository;
    private readonly IHolidayService _holidayService;

    public DeadlineCalculator(ICourtRuleRepository courtRuleRepository, IHolidayService holidayService)
    {
        _courtRuleRepository = courtRuleRepository;
        _holidayService = holidayService;
    }

    public async Task<IEnumerable<Deadline>> CalculateDeadlinesAsync(
        Matter matter,
        TriggerEvent triggerEvent,
        CancellationToken cancellationToken = default)
    {
        var deadlines = new List<Deadline>();

        var rules = await _courtRuleRepository.GetRulesByTriggerTypeAsync(
            matter.State,
            matter.County,
            matter.CaseType,
            triggerEvent.EventType,
            cancellationToken);

        foreach (var rule in rules)
        {
            var courtDeadlineDueDate = CalculateDueDate(
                triggerEvent.EventDate,
                rule.DaysFromTrigger,
                rule.CountBusinessDays,
                rule.ExcludeHolidays,
                matter.State);

            var courtDeadline = new Deadline
            {
                Id = Guid.NewGuid(),
                MatterId = matter.Id,
                CourtRuleId = rule.Id,
                TriggerEventId = triggerEvent.Id,
                Type = DeadlineType.Court,
                Title = rule.RuleName,
                Description = rule.RuleDescription,
                DueDate = courtDeadlineDueDate,
                CalculatedDueDate = courtDeadlineDueDate,
                Status = DeadlineStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            deadlines.Add(courtDeadline);

            if (rule.BufferDays.HasValue && rule.BufferDays.Value > 0)
            {
                var bufferDueDate = CalculateBufferDate(
                    courtDeadlineDueDate,
                    rule.BufferDays.Value,
                    rule.CountBusinessDays,
                    matter.State);

                var bufferDeadline = new Deadline
                {
                    Id = Guid.NewGuid(),
                    MatterId = matter.Id,
                    CourtRuleId = rule.Id,
                    TriggerEventId = triggerEvent.Id,
                    Type = DeadlineType.Buffer,
                    Title = $"[BUFFER] {rule.RuleName}",
                    Description = $"Internal deadline - {rule.BufferDays} days before court deadline",
                    DueDate = bufferDueDate,
                    CalculatedDueDate = bufferDueDate,
                    Status = DeadlineStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                deadlines.Add(bufferDeadline);
            }
        }

        return deadlines;
    }

    public async Task<Deadline> RecalculateDeadlineAsync(
        Deadline deadline,
        DateTime newTriggerDate,
        CancellationToken cancellationToken = default)
    {
        if (deadline.CourtRule == null)
        {
            throw new InvalidOperationException("Cannot recalculate deadline without associated court rule");
        }

        var rule = deadline.CourtRule;
        var state = deadline.Matter?.State;

        var newDueDate = CalculateDueDate(
            newTriggerDate,
            rule.DaysFromTrigger,
            rule.CountBusinessDays,
            rule.ExcludeHolidays,
            state);

        if (deadline.Type == DeadlineType.Buffer && rule.BufferDays.HasValue)
        {
            var courtDeadlineDate = CalculateDueDate(
                newTriggerDate,
                rule.DaysFromTrigger,
                rule.CountBusinessDays,
                rule.ExcludeHolidays,
                state);

            newDueDate = CalculateBufferDate(
                courtDeadlineDate,
                rule.BufferDays.Value,
                rule.CountBusinessDays,
                state);
        }

        deadline.CalculatedDueDate = newDueDate;

        if (!deadline.IsManualOverride)
        {
            deadline.DueDate = newDueDate;
        }

        deadline.UpdatedAt = DateTime.UtcNow;

        return deadline;
    }

    public DateTime CalculateDueDate(
        DateTime triggerDate,
        int daysFromTrigger,
        bool countBusinessDays,
        bool excludeHolidays,
        string? state = null)
    {
        if (!countBusinessDays)
        {
            return triggerDate.AddDays(daysFromTrigger);
        }

        var currentDate = triggerDate;
        var daysAdded = 0;
        var direction = daysFromTrigger >= 0 ? 1 : -1;
        var targetDays = Math.Abs(daysFromTrigger);

        while (daysAdded < targetDays)
        {
            currentDate = currentDate.AddDays(direction);

            if (_holidayService.IsBusinessDay(currentDate))
            {
                daysAdded++;
            }
        }

        while (!_holidayService.IsBusinessDay(currentDate))
        {
            currentDate = _holidayService.GetNextBusinessDay(currentDate);
        }

        return currentDate;
    }

    public DateTime CalculateBufferDate(
        DateTime dueDate,
        int bufferDays,
        bool countBusinessDays,
        string? state = null)
    {
        return CalculateDueDate(dueDate, -bufferDays, countBusinessDays, true, state);
    }
}
