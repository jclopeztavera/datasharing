using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;
using DeadlineApp.Core.Interfaces;
using DeadlineApp.Core.Services;
using FluentAssertions;
using Moq;

namespace DeadlineApp.Core.Tests;

public class DeadlineCalculatorTests
{
    private readonly Mock<ICourtRuleRepository> _mockRuleRepo;
    private readonly HolidayService _holidayService;
    private readonly DeadlineCalculator _sut;

    public DeadlineCalculatorTests()
    {
        _mockRuleRepo = new Mock<ICourtRuleRepository>();
        _holidayService = new HolidayService();
        _sut = new DeadlineCalculator(_mockRuleRepo.Object, _holidayService);
    }

    private static Matter CreateTestMatter(string state = "FL", string county = "Miami-Dade", string caseType = "Divorce")
    {
        return new Matter
        {
            Id = Guid.NewGuid(),
            FirmId = Guid.NewGuid(),
            MatterNumber = "FL-2026-001",
            Title = "Test Matter",
            State = state,
            County = county,
            CaseType = caseType,
            FilingDate = new DateTime(2026, 1, 15),
            ResponsibleAttorneyId = Guid.NewGuid(),
            Status = MatterStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static TriggerEvent CreateTestTriggerEvent(Guid matterId, DateTime eventDate, TriggerEventType type = TriggerEventType.FilingDate)
    {
        return new TriggerEvent
        {
            Id = Guid.NewGuid(),
            MatterId = matterId,
            EventType = type,
            EventDate = eventDate,
            OriginalEventDate = eventDate,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static CourtRule CreateTestRule(
        string state = "FL",
        string caseType = "Divorce",
        int daysFromTrigger = 20,
        bool countBusinessDays = false,
        int? bufferDays = null,
        TriggerEventType triggerType = TriggerEventType.FilingDate)
    {
        return new CourtRule
        {
            Id = Guid.NewGuid(),
            State = state,
            CaseType = caseType,
            RuleName = "Test Rule - Response Deadline",
            RuleDescription = "File response within specified days",
            TriggerEventType = triggerType,
            DaysFromTrigger = daysFromTrigger,
            CountBusinessDays = countBusinessDays,
            ExcludeHolidays = true,
            BufferDays = bufferDays,
            IsActive = true,
            EffectiveDate = new DateTime(2020, 1, 1),
            CreatedAt = DateTime.UtcNow
        };
    }

    // --- CalculateDueDate (calendar days) ---

    [Fact]
    public void CalculateDueDate_CalendarDays_AddsCorrectDays()
    {
        var triggerDate = new DateTime(2026, 1, 15); // Thursday
        var result = _sut.CalculateDueDate(triggerDate, 20, countBusinessDays: false, excludeHolidays: false);
        result.Should().Be(new DateTime(2026, 2, 4));
    }

    [Fact]
    public void CalculateDueDate_CalendarDays_30Days()
    {
        var triggerDate = new DateTime(2026, 3, 1);
        var result = _sut.CalculateDueDate(triggerDate, 30, countBusinessDays: false, excludeHolidays: false);
        result.Should().Be(new DateTime(2026, 3, 31));
    }

    [Fact]
    public void CalculateDueDate_CalendarDays_CrossesMonthBoundary()
    {
        var triggerDate = new DateTime(2026, 1, 20);
        var result = _sut.CalculateDueDate(triggerDate, 15, countBusinessDays: false, excludeHolidays: false);
        result.Should().Be(new DateTime(2026, 2, 4));
    }

    // --- CalculateDueDate (business days) ---

    [Fact]
    public void CalculateDueDate_BusinessDays_SkipsWeekends()
    {
        // Jan 5, 2026 is Monday. 5 business days = Jan 12 (Monday)
        var triggerDate = new DateTime(2026, 1, 5);
        var result = _sut.CalculateDueDate(triggerDate, 5, countBusinessDays: true, excludeHolidays: true);
        result.Should().Be(new DateTime(2026, 1, 12));
    }

    [Fact]
    public void CalculateDueDate_BusinessDays_10Days()
    {
        // Jan 5, 2026 is Monday. 10 business days = Jan 19 (Monday, but MLK Day)
        // MLK Day is a federal holiday, so should skip to Jan 20 (Tuesday)
        var triggerDate = new DateTime(2026, 1, 5);
        var result = _sut.CalculateDueDate(triggerDate, 10, countBusinessDays: true, excludeHolidays: true);
        result.Should().Be(new DateTime(2026, 1, 20));
    }

    [Fact]
    public void CalculateDueDate_BusinessDays_SkipsHoliday()
    {
        // Starting from Dec 21, 2026 (Monday), counting 5 business days
        // Dec 22 (Tue), Dec 23 (Wed), Dec 24 (Thu), Dec 25 (Fri - Christmas, skip),
        // Dec 26-27 (weekend), Dec 28 (Mon) = 4th business day, Dec 29 (Tue) = 5th
        var triggerDate = new DateTime(2026, 12, 21);
        var result = _sut.CalculateDueDate(triggerDate, 5, countBusinessDays: true, excludeHolidays: true);
        result.Should().Be(new DateTime(2026, 12, 29));
    }

    [Fact]
    public void CalculateDueDate_BusinessDays_1Day()
    {
        var monday = new DateTime(2026, 1, 5);
        var result = _sut.CalculateDueDate(monday, 1, countBusinessDays: true, excludeHolidays: true);
        result.Should().Be(new DateTime(2026, 1, 6)); // Tuesday
    }

    [Fact]
    public void CalculateDueDate_BusinessDays_FromFriday()
    {
        var friday = new DateTime(2026, 1, 9);
        var result = _sut.CalculateDueDate(friday, 1, countBusinessDays: true, excludeHolidays: true);
        result.Should().Be(new DateTime(2026, 1, 12)); // Monday
    }

    // --- CalculateBufferDate ---

    [Fact]
    public void CalculateBufferDate_CalendarDays_SubtractsCorrectly()
    {
        var dueDate = new DateTime(2026, 2, 4);
        var result = _sut.CalculateBufferDate(dueDate, 5, countBusinessDays: false);
        result.Should().Be(new DateTime(2026, 1, 30));
    }

    [Fact]
    public void CalculateBufferDate_BusinessDays_SkipsWeekends()
    {
        // Due date is Monday Jan 12. 3 business days before = Wednesday Jan 7
        var dueDate = new DateTime(2026, 1, 12);
        var result = _sut.CalculateBufferDate(dueDate, 3, countBusinessDays: true);
        result.Should().Be(new DateTime(2026, 1, 7));
    }

    // --- CalculateDeadlinesAsync ---

    [Fact]
    public async Task CalculateDeadlinesAsync_SingleRule_CreatesCourtDeadline()
    {
        var matter = CreateTestMatter();
        var triggerEvent = CreateTestTriggerEvent(matter.Id, new DateTime(2026, 1, 15));
        var rule = CreateTestRule(daysFromTrigger: 20);

        _mockRuleRepo
            .Setup(r => r.GetRulesByTriggerTypeAsync("FL", "Miami-Dade", "Divorce", TriggerEventType.FilingDate, default))
            .ReturnsAsync(new[] { rule });

        var deadlines = (await _sut.CalculateDeadlinesAsync(matter, triggerEvent)).ToList();

        deadlines.Should().HaveCount(1);
        deadlines[0].Type.Should().Be(DeadlineType.Court);
        deadlines[0].Title.Should().Be(rule.RuleName);
        deadlines[0].MatterId.Should().Be(matter.Id);
        deadlines[0].CourtRuleId.Should().Be(rule.Id);
        deadlines[0].TriggerEventId.Should().Be(triggerEvent.Id);
        deadlines[0].Status.Should().Be(DeadlineStatus.Pending);
        deadlines[0].DueDate.Should().Be(new DateTime(2026, 2, 4));
    }

    [Fact]
    public async Task CalculateDeadlinesAsync_RuleWithBuffer_CreatesCourtAndBufferDeadlines()
    {
        var matter = CreateTestMatter();
        var triggerEvent = CreateTestTriggerEvent(matter.Id, new DateTime(2026, 1, 15));
        var rule = CreateTestRule(daysFromTrigger: 20, bufferDays: 5);

        _mockRuleRepo
            .Setup(r => r.GetRulesByTriggerTypeAsync("FL", "Miami-Dade", "Divorce", TriggerEventType.FilingDate, default))
            .ReturnsAsync(new[] { rule });

        var deadlines = (await _sut.CalculateDeadlinesAsync(matter, triggerEvent)).ToList();

        deadlines.Should().HaveCount(2);

        var courtDeadline = deadlines.First(d => d.Type == DeadlineType.Court);
        var bufferDeadline = deadlines.First(d => d.Type == DeadlineType.Buffer);

        courtDeadline.DueDate.Should().Be(new DateTime(2026, 2, 4));
        bufferDeadline.DueDate.Should().BeBefore(courtDeadline.DueDate);
        bufferDeadline.Title.Should().StartWith("[BUFFER]");
    }

    [Fact]
    public async Task CalculateDeadlinesAsync_MultipleRules_CreatesDeadlinePerRule()
    {
        var matter = CreateTestMatter();
        var triggerEvent = CreateTestTriggerEvent(matter.Id, new DateTime(2026, 1, 15));
        var rule1 = CreateTestRule(daysFromTrigger: 20);
        rule1.RuleName = "Answer Deadline";
        var rule2 = CreateTestRule(daysFromTrigger: 30);
        rule2.RuleName = "Discovery Deadline";

        _mockRuleRepo
            .Setup(r => r.GetRulesByTriggerTypeAsync("FL", "Miami-Dade", "Divorce", TriggerEventType.FilingDate, default))
            .ReturnsAsync(new[] { rule1, rule2 });

        var deadlines = (await _sut.CalculateDeadlinesAsync(matter, triggerEvent)).ToList();

        deadlines.Should().HaveCount(2);
        deadlines.Should().Contain(d => d.Title == "Answer Deadline");
        deadlines.Should().Contain(d => d.Title == "Discovery Deadline");
    }

    [Fact]
    public async Task CalculateDeadlinesAsync_NoMatchingRules_ReturnsEmpty()
    {
        var matter = CreateTestMatter();
        var triggerEvent = CreateTestTriggerEvent(matter.Id, new DateTime(2026, 1, 15));

        _mockRuleRepo
            .Setup(r => r.GetRulesByTriggerTypeAsync("FL", "Miami-Dade", "Divorce", TriggerEventType.FilingDate, default))
            .ReturnsAsync(Array.Empty<CourtRule>());

        var deadlines = await _sut.CalculateDeadlinesAsync(matter, triggerEvent);

        deadlines.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateDeadlinesAsync_BusinessDayRule_CalculatesCorrectly()
    {
        var matter = CreateTestMatter();
        var triggerEvent = CreateTestTriggerEvent(matter.Id, new DateTime(2026, 1, 5)); // Monday
        var rule = CreateTestRule(daysFromTrigger: 5, countBusinessDays: true);

        _mockRuleRepo
            .Setup(r => r.GetRulesByTriggerTypeAsync("FL", "Miami-Dade", "Divorce", TriggerEventType.FilingDate, default))
            .ReturnsAsync(new[] { rule });

        var deadlines = (await _sut.CalculateDeadlinesAsync(matter, triggerEvent)).ToList();

        deadlines.Should().HaveCount(1);
        deadlines[0].DueDate.Should().Be(new DateTime(2026, 1, 12)); // 5 business days from Monday = next Monday
    }

    // --- RecalculateDeadlineAsync ---

    [Fact]
    public async Task RecalculateDeadlineAsync_UpdatesDueDate()
    {
        var rule = CreateTestRule(daysFromTrigger: 20);
        var matter = CreateTestMatter();

        var deadline = new Deadline
        {
            Id = Guid.NewGuid(),
            MatterId = matter.Id,
            Matter = matter,
            CourtRule = rule,
            CourtRuleId = rule.Id,
            Type = DeadlineType.Court,
            Title = "Test Deadline",
            DueDate = new DateTime(2026, 2, 4),
            CalculatedDueDate = new DateTime(2026, 2, 4),
            Status = DeadlineStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var newTriggerDate = new DateTime(2026, 1, 20); // 5 days later

        var result = await _sut.RecalculateDeadlineAsync(deadline, newTriggerDate);

        result.DueDate.Should().Be(new DateTime(2026, 2, 9)); // 20 calendar days from Jan 20
        result.CalculatedDueDate.Should().Be(new DateTime(2026, 2, 9));
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RecalculateDeadlineAsync_ManualOverride_UpdatesCalculatedButNotDueDate()
    {
        var rule = CreateTestRule(daysFromTrigger: 20);
        var matter = CreateTestMatter();
        var overriddenDate = new DateTime(2026, 3, 1);

        var deadline = new Deadline
        {
            Id = Guid.NewGuid(),
            MatterId = matter.Id,
            Matter = matter,
            CourtRule = rule,
            CourtRuleId = rule.Id,
            Type = DeadlineType.Court,
            Title = "Test Deadline",
            DueDate = overriddenDate, // Manually overridden
            CalculatedDueDate = new DateTime(2026, 2, 4),
            IsManualOverride = true,
            OverrideReason = "Judge extended deadline",
            Status = DeadlineStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var newTriggerDate = new DateTime(2026, 1, 20);

        var result = await _sut.RecalculateDeadlineAsync(deadline, newTriggerDate);

        // CalculatedDueDate should update
        result.CalculatedDueDate.Should().Be(new DateTime(2026, 2, 9));
        // DueDate should remain the overridden date
        result.DueDate.Should().Be(overriddenDate);
        result.IsManualOverride.Should().BeTrue();
    }

    [Fact]
    public async Task RecalculateDeadlineAsync_NoCourtRule_ThrowsException()
    {
        var deadline = new Deadline
        {
            Id = Guid.NewGuid(),
            MatterId = Guid.NewGuid(),
            CourtRule = null,
            Type = DeadlineType.Court,
            Title = "Test Deadline",
            DueDate = DateTime.UtcNow.AddDays(20),
            CalculatedDueDate = DateTime.UtcNow.AddDays(20),
            Status = DeadlineStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var act = () => _sut.RecalculateDeadlineAsync(deadline, DateTime.UtcNow);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*court rule*");
    }

    [Fact]
    public async Task RecalculateDeadlineAsync_BufferDeadline_RecalculatesFromCourtDeadline()
    {
        var rule = CreateTestRule(daysFromTrigger: 20, bufferDays: 5);
        var matter = CreateTestMatter();

        var deadline = new Deadline
        {
            Id = Guid.NewGuid(),
            MatterId = matter.Id,
            Matter = matter,
            CourtRule = rule,
            CourtRuleId = rule.Id,
            Type = DeadlineType.Buffer,
            Title = "[BUFFER] Test Rule",
            DueDate = new DateTime(2026, 1, 30),
            CalculatedDueDate = new DateTime(2026, 1, 30),
            Status = DeadlineStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var newTriggerDate = new DateTime(2026, 1, 20);

        var result = await _sut.RecalculateDeadlineAsync(deadline, newTriggerDate);

        // Court deadline would be Feb 9. Buffer = 5 days before = Feb 4.
        result.DueDate.Should().Be(new DateTime(2026, 2, 4));
    }

    // --- Edge cases ---

    [Fact]
    public void CalculateDueDate_ZeroDays_ReturnsSameDate()
    {
        var triggerDate = new DateTime(2026, 1, 15);
        var result = _sut.CalculateDueDate(triggerDate, 0, countBusinessDays: false, excludeHolidays: false);
        result.Should().Be(triggerDate);
    }

    [Fact]
    public async Task CalculateDeadlinesAsync_DifferentTriggerType_QueriesCorrectType()
    {
        var matter = CreateTestMatter();
        var triggerEvent = CreateTestTriggerEvent(matter.Id, new DateTime(2026, 1, 15), TriggerEventType.HearingDate);
        var rule = CreateTestRule(triggerType: TriggerEventType.HearingDate, daysFromTrigger: 10);

        _mockRuleRepo
            .Setup(r => r.GetRulesByTriggerTypeAsync("FL", "Miami-Dade", "Divorce", TriggerEventType.HearingDate, default))
            .ReturnsAsync(new[] { rule });

        var deadlines = (await _sut.CalculateDeadlinesAsync(matter, triggerEvent)).ToList();

        deadlines.Should().HaveCount(1);
        _mockRuleRepo.Verify(
            r => r.GetRulesByTriggerTypeAsync("FL", "Miami-Dade", "Divorce", TriggerEventType.HearingDate, default),
            Times.Once);
    }
}
