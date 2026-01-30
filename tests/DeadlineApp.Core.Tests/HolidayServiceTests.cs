using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Services;
using FluentAssertions;

namespace DeadlineApp.Core.Tests;

public class HolidayServiceTests
{
    private readonly HolidayService _sut;

    public HolidayServiceTests()
    {
        _sut = new HolidayService();
    }

    // --- IsBusinessDay ---

    [Theory]
    [InlineData(2026, 1, 5)]  // Monday
    [InlineData(2026, 1, 6)]  // Tuesday
    [InlineData(2026, 1, 7)]  // Wednesday
    [InlineData(2026, 1, 8)]  // Thursday
    [InlineData(2026, 1, 9)]  // Friday
    public void IsBusinessDay_Weekday_ReturnsTrue(int year, int month, int day)
    {
        var date = new DateTime(year, month, day);
        _sut.IsBusinessDay(date).Should().BeTrue();
    }

    [Theory]
    [InlineData(2026, 1, 3)]  // Saturday
    [InlineData(2026, 1, 4)]  // Sunday
    public void IsBusinessDay_Weekend_ReturnsFalse(int year, int month, int day)
    {
        var date = new DateTime(year, month, day);
        _sut.IsBusinessDay(date).Should().BeFalse();
    }

    [Fact]
    public void IsBusinessDay_ChristmasDay_ReturnsFalse()
    {
        var christmas = new DateTime(2026, 12, 25);
        _sut.IsBusinessDay(christmas).Should().BeFalse();
    }

    [Fact]
    public void IsBusinessDay_NewYearsDay_ReturnsFalse()
    {
        var newYear = new DateTime(2026, 1, 1);
        _sut.IsBusinessDay(newYear).Should().BeFalse();
    }

    [Fact]
    public void IsBusinessDay_IndependenceDay_ReturnsFalse()
    {
        var july4 = new DateTime(2026, 7, 4); // Saturday in 2026, but still a holiday
        _sut.IsBusinessDay(july4).Should().BeFalse();
    }

    [Fact]
    public void IsBusinessDay_WithCustomHoliday_ReturnsFalse()
    {
        var date = new DateTime(2026, 3, 15); // a regular weekday
        var holidays = new List<Holiday>
        {
            new Holiday { Name = "Custom Holiday", Date = date, IsCourtClosed = true }
        };

        _sut.IsBusinessDay(date, holidays).Should().BeFalse();
    }

    [Fact]
    public void IsBusinessDay_WithNonClosingHoliday_ReturnsTrue()
    {
        var date = new DateTime(2026, 3, 16); // Monday
        var holidays = new List<Holiday>
        {
            new Holiday { Name = "Observance", Date = date, IsCourtClosed = false }
        };

        _sut.IsBusinessDay(date, holidays).Should().BeTrue();
    }

    // --- IsHoliday ---

    [Fact]
    public void IsHoliday_DateMatchesHoliday_ReturnsTrue()
    {
        var date = new DateTime(2026, 12, 25);
        var holidays = new List<Holiday>
        {
            new Holiday { Name = "Christmas", Date = date, IsCourtClosed = true }
        };

        _sut.IsHoliday(date, holidays).Should().BeTrue();
    }

    [Fact]
    public void IsHoliday_DateDoesNotMatch_ReturnsFalse()
    {
        var date = new DateTime(2026, 3, 15);
        var holidays = new List<Holiday>
        {
            new Holiday { Name = "Christmas", Date = new DateTime(2026, 12, 25), IsCourtClosed = true }
        };

        _sut.IsHoliday(date, holidays).Should().BeFalse();
    }

    [Fact]
    public void IsHoliday_CourtNotClosed_ReturnsFalse()
    {
        var date = new DateTime(2026, 3, 15);
        var holidays = new List<Holiday>
        {
            new Holiday { Name = "Observance", Date = date, IsCourtClosed = false }
        };

        _sut.IsHoliday(date, holidays).Should().BeFalse();
    }

    // --- GetNextBusinessDay ---

    [Fact]
    public void GetNextBusinessDay_FromFriday_ReturnsMonday()
    {
        var friday = new DateTime(2026, 1, 9); // Friday
        var result = _sut.GetNextBusinessDay(friday);
        result.Should().Be(new DateTime(2026, 1, 12)); // Monday
    }

    [Fact]
    public void GetNextBusinessDay_FromSaturday_ReturnsMonday()
    {
        var saturday = new DateTime(2026, 1, 10); // Saturday
        var result = _sut.GetNextBusinessDay(saturday);
        result.Should().Be(new DateTime(2026, 1, 12)); // Monday
    }

    [Fact]
    public void GetNextBusinessDay_FromMonday_ReturnsTuesday()
    {
        var monday = new DateTime(2026, 1, 5); // Monday
        var result = _sut.GetNextBusinessDay(monday);
        result.Should().Be(new DateTime(2026, 1, 6)); // Tuesday
    }

    [Fact]
    public void GetNextBusinessDay_BeforeHoliday_SkipsHoliday()
    {
        // Dec 24, 2026 is Thursday. Dec 25 is Friday (Christmas).
        var thursday = new DateTime(2026, 12, 24);
        var result = _sut.GetNextBusinessDay(thursday);
        // Christmas is a federal holiday, so skip to Monday Dec 28
        result.Should().Be(new DateTime(2026, 12, 28));
    }

    // --- GetHolidaysAsync ---

    [Fact]
    public async Task GetHolidaysAsync_ReturnsHolidaysInRange()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 12, 31);

        var holidays = await _sut.GetHolidaysAsync(from, to);

        holidays.Should().NotBeEmpty();
        holidays.Should().Contain(h => h.Name == "Independence Day");
        holidays.Should().Contain(h => h.Name == "Christmas Day");
        holidays.Should().Contain(h => h.Name == "Thanksgiving");
    }

    [Fact]
    public async Task GetHolidaysAsync_NarrowRange_ReturnsOnlyMatchingHolidays()
    {
        var from = new DateTime(2026, 7, 1);
        var to = new DateTime(2026, 7, 31);

        var holidays = await _sut.GetHolidaysAsync(from, to);

        holidays.Should().Contain(h => h.Name == "Independence Day");
        holidays.Should().NotContain(h => h.Name == "Christmas Day");
    }

    [Fact]
    public async Task GetHolidaysAsync_WithStateFilter_ReturnsFederalHolidays()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 12, 31);

        var holidays = await _sut.GetHolidaysAsync(from, to, "FL");

        // Federal holidays have State = null, so they match any state filter
        holidays.Should().NotBeEmpty();
    }

    // --- Known holiday date calculations ---

    [Fact]
    public void MLKDay2026_IsThirdMondayOfJanuary()
    {
        // MLK Day 2026 should be January 19
        var mlkDay = new DateTime(2026, 1, 19);
        mlkDay.DayOfWeek.Should().Be(DayOfWeek.Monday);
        _sut.IsBusinessDay(mlkDay).Should().BeFalse();
    }

    [Fact]
    public void Thanksgiving2026_IsFourthThursdayOfNovember()
    {
        // Thanksgiving 2026 is November 26
        var thanksgiving = new DateTime(2026, 11, 26);
        thanksgiving.DayOfWeek.Should().Be(DayOfWeek.Thursday);
        _sut.IsBusinessDay(thanksgiving).Should().BeFalse();
    }

    [Fact]
    public void LaborDay2026_IsFirstMondayOfSeptember()
    {
        // Labor Day 2026 is September 7
        var laborDay = new DateTime(2026, 9, 7);
        laborDay.DayOfWeek.Should().Be(DayOfWeek.Monday);
        _sut.IsBusinessDay(laborDay).Should().BeFalse();
    }

    [Fact]
    public void MemorialDay2026_IsLastMondayOfMay()
    {
        // Memorial Day 2026 is May 25
        var memorialDay = new DateTime(2026, 5, 25);
        memorialDay.DayOfWeek.Should().Be(DayOfWeek.Monday);
        _sut.IsBusinessDay(memorialDay).Should().BeFalse();
    }
}
