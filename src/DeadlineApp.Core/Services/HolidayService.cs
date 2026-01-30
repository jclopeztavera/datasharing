using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Interfaces;

namespace DeadlineApp.Core.Services;

public class HolidayService : IHolidayService
{
    private readonly List<Holiday> _federalHolidays;

    public HolidayService()
    {
        _federalHolidays = GenerateFederalHolidays(DateTime.UtcNow.Year - 1, DateTime.UtcNow.Year + 5);
    }

    public Task<IEnumerable<Holiday>> GetHolidaysAsync(
        DateTime fromDate,
        DateTime toDate,
        string? state = null,
        CancellationToken cancellationToken = default)
    {
        var holidays = _federalHolidays
            .Where(h => h.Date >= fromDate && h.Date <= toDate)
            .AsEnumerable();

        if (!string.IsNullOrEmpty(state))
        {
            holidays = holidays.Where(h => h.State == null || h.State == state);
        }

        return Task.FromResult(holidays);
    }

    public bool IsHoliday(DateTime date, IEnumerable<Holiday> holidays)
    {
        return holidays.Any(h => h.Date.Date == date.Date && h.IsCourtClosed);
    }

    public bool IsBusinessDay(DateTime date, IEnumerable<Holiday>? holidays = null)
    {
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
        {
            return false;
        }

        if (holidays != null && IsHoliday(date, holidays))
        {
            return false;
        }

        var federalHoliday = _federalHolidays.FirstOrDefault(h => h.Date.Date == date.Date);
        if (federalHoliday != null && federalHoliday.IsCourtClosed)
        {
            return false;
        }

        return true;
    }

    public DateTime GetNextBusinessDay(DateTime date, IEnumerable<Holiday>? holidays = null)
    {
        var nextDate = date.AddDays(1);
        while (!IsBusinessDay(nextDate, holidays))
        {
            nextDate = nextDate.AddDays(1);
        }
        return nextDate;
    }

    private static List<Holiday> GenerateFederalHolidays(int startYear, int endYear)
    {
        var holidays = new List<Holiday>();

        for (var year = startYear; year <= endYear; year++)
        {
            holidays.Add(new Holiday { Name = "New Year's Day", Date = new DateTime(year, 1, 1), IsFederal = true });
            holidays.Add(new Holiday { Name = "Martin Luther King Jr. Day", Date = GetNthDayOfMonth(year, 1, DayOfWeek.Monday, 3), IsFederal = true });
            holidays.Add(new Holiday { Name = "Presidents' Day", Date = GetNthDayOfMonth(year, 2, DayOfWeek.Monday, 3), IsFederal = true });
            holidays.Add(new Holiday { Name = "Memorial Day", Date = GetLastDayOfMonth(year, 5, DayOfWeek.Monday), IsFederal = true });
            holidays.Add(new Holiday { Name = "Juneteenth", Date = new DateTime(year, 6, 19), IsFederal = true });
            holidays.Add(new Holiday { Name = "Independence Day", Date = new DateTime(year, 7, 4), IsFederal = true });
            holidays.Add(new Holiday { Name = "Labor Day", Date = GetNthDayOfMonth(year, 9, DayOfWeek.Monday, 1), IsFederal = true });
            holidays.Add(new Holiday { Name = "Columbus Day", Date = GetNthDayOfMonth(year, 10, DayOfWeek.Monday, 2), IsFederal = true });
            holidays.Add(new Holiday { Name = "Veterans Day", Date = new DateTime(year, 11, 11), IsFederal = true });
            holidays.Add(new Holiday { Name = "Thanksgiving", Date = GetNthDayOfMonth(year, 11, DayOfWeek.Thursday, 4), IsFederal = true });
            holidays.Add(new Holiday { Name = "Christmas Day", Date = new DateTime(year, 12, 25), IsFederal = true });
        }

        return holidays;
    }

    private static DateTime GetNthDayOfMonth(int year, int month, DayOfWeek dayOfWeek, int n)
    {
        var firstDay = new DateTime(year, month, 1);
        var daysUntil = ((int)dayOfWeek - (int)firstDay.DayOfWeek + 7) % 7;
        return firstDay.AddDays(daysUntil + (n - 1) * 7);
    }

    private static DateTime GetLastDayOfMonth(int year, int month, DayOfWeek dayOfWeek)
    {
        var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        var daysBack = ((int)lastDay.DayOfWeek - (int)dayOfWeek + 7) % 7;
        return lastDay.AddDays(-daysBack);
    }
}
