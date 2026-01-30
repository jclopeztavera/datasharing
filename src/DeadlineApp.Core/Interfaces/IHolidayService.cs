using DeadlineApp.Core.Entities;

namespace DeadlineApp.Core.Interfaces;

public interface IHolidayService
{
    Task<IEnumerable<Holiday>> GetHolidaysAsync(DateTime fromDate, DateTime toDate, string? state = null, CancellationToken cancellationToken = default);
    bool IsHoliday(DateTime date, IEnumerable<Holiday> holidays);
    bool IsBusinessDay(DateTime date, IEnumerable<Holiday>? holidays = null);
    DateTime GetNextBusinessDay(DateTime date, IEnumerable<Holiday>? holidays = null);
}
