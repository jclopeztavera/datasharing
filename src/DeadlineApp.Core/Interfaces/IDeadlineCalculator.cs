using DeadlineApp.Core.Entities;

namespace DeadlineApp.Core.Interfaces;

public interface IDeadlineCalculator
{
    Task<IEnumerable<Deadline>> CalculateDeadlinesAsync(Matter matter, TriggerEvent triggerEvent, CancellationToken cancellationToken = default);
    Task<Deadline> RecalculateDeadlineAsync(Deadline deadline, DateTime newTriggerDate, CancellationToken cancellationToken = default);
    DateTime CalculateDueDate(DateTime triggerDate, int daysFromTrigger, bool countBusinessDays, bool excludeHolidays, string? state = null);
    DateTime CalculateBufferDate(DateTime dueDate, int bufferDays, bool countBusinessDays, string? state = null);
}
