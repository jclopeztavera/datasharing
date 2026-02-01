using DeadlineApp.Core.Models;
using DeadlineApp.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace DeadlineApp.Infrastructure.Tests;

public class EmailNotificationServiceTests
{
    [Fact]
    public void BuildOverdueAlertHtml_ContainsDeadlineTitle()
    {
        var notification = CreateSampleNotification(daysUntilDue: -3, isOverdue: true);
        var html = EmailNotificationService.BuildOverdueAlertHtml(notification, "Jane Doe");

        html.Should().Contain("Response to Petition");
        html.Should().Contain("3 day(s) past due");
        html.Should().Contain("Jane Doe");
    }

    [Fact]
    public void BuildOverdueAlertHtml_ContainsMatterInfo()
    {
        var notification = CreateSampleNotification(daysUntilDue: -1, isOverdue: true);
        var html = EmailNotificationService.BuildOverdueAlertHtml(notification, "John");

        html.Should().Contain("MAT-001");
        html.Should().Contain("Smith v. Smith");
    }

    [Fact]
    public void BuildOverdueAlertHtml_ContainsRuleCitation_WhenProvided()
    {
        var notification = CreateSampleNotification(daysUntilDue: -2, isOverdue: true);
        notification.CourtRuleCitation = "Fla. Fam. L. R. P. 12.140(a)";

        var html = EmailNotificationService.BuildOverdueAlertHtml(notification, "Jane");

        html.Should().Contain("Fla. Fam. L. R. P. 12.140(a)");
    }

    [Fact]
    public void BuildOverdueAlertHtml_OmitsRuleRow_WhenNoCitation()
    {
        var notification = CreateSampleNotification(daysUntilDue: -1, isOverdue: true);
        notification.CourtRuleCitation = null;

        var html = EmailNotificationService.BuildOverdueAlertHtml(notification, "Jane");

        html.Should().NotContain("Rule</td>");
    }

    [Fact]
    public void BuildOverdueAlertHtml_EscapesHtmlCharacters()
    {
        var notification = CreateSampleNotification(daysUntilDue: -1, isOverdue: true);
        notification.MatterTitle = "O'Brien & Associates <LLC>";

        var html = EmailNotificationService.BuildOverdueAlertHtml(notification, "Bob & \"Sue\"");

        html.Should().Contain("Bob &amp; &quot;Sue&quot;");
        html.Should().Contain("O&#39;Brien &amp; Associates &lt;LLC&gt;");
    }

    [Fact]
    public void BuildDailySummaryHtml_ContainsAttorneyGreeting()
    {
        var summary = CreateSampleSummary();
        var html = EmailNotificationService.BuildDailySummaryHtml(summary);

        html.Should().Contain("Hi Jane Doe");
        html.Should().Contain("Daily Deadline Summary");
    }

    [Fact]
    public void BuildDailySummaryHtml_ContainsOverdueSection_WhenOverduePresent()
    {
        var summary = CreateSampleSummary();
        summary.OverdueDeadlines.Add(CreateSampleNotification(-2, isOverdue: true));

        var html = EmailNotificationService.BuildDailySummaryHtml(summary);

        html.Should().Contain("Overdue");
        html.Should().Contain("color: #d13438");
    }

    [Fact]
    public void BuildDailySummaryHtml_ContainsAtRiskSection_WhenAtRiskPresent()
    {
        var summary = CreateSampleSummary();
        summary.AtRiskDeadlines.Add(CreateSampleNotification(2, isAtRisk: true));

        var html = EmailNotificationService.BuildDailySummaryHtml(summary);

        html.Should().Contain("At Risk (Buffer)");
        html.Should().Contain("color: #ca5010");
    }

    [Fact]
    public void BuildDailySummaryHtml_ContainsUpcomingSection_WhenUpcomingPresent()
    {
        var summary = CreateSampleSummary();
        summary.UpcomingDeadlines.Add(CreateSampleNotification(5));

        var html = EmailNotificationService.BuildDailySummaryHtml(summary);

        html.Should().Contain("Upcoming (Next 7 Days)");
        html.Should().Contain("color: #0078d4");
    }

    [Fact]
    public void BuildDailySummaryHtml_ShowsAllClearMessage_WhenEmpty()
    {
        var summary = CreateSampleSummary();
        // No deadlines added

        var html = EmailNotificationService.BuildDailySummaryHtml(summary);

        html.Should().Contain("No pending deadlines");
    }

    [Fact]
    public void BuildDailySummaryHtml_ShowsCounts()
    {
        var summary = CreateSampleSummary();
        summary.OverdueDeadlines.Add(CreateSampleNotification(-1, isOverdue: true));
        summary.OverdueDeadlines.Add(CreateSampleNotification(-3, isOverdue: true));
        summary.AtRiskDeadlines.Add(CreateSampleNotification(1, isAtRisk: true));
        summary.UpcomingDeadlines.Add(CreateSampleNotification(5));

        var html = EmailNotificationService.BuildDailySummaryHtml(summary);

        html.Should().Contain("2 overdue");
        html.Should().Contain("1 at risk");
        html.Should().Contain("1 upcoming");
    }

    [Fact]
    public void BuildDailySummaryHtml_ContainsDueDateFormatted()
    {
        var summary = CreateSampleSummary();
        var deadline = CreateSampleNotification(3);
        deadline.DueDate = new DateTime(2026, 3, 15);
        summary.UpcomingDeadlines.Add(deadline);

        var html = EmailNotificationService.BuildDailySummaryHtml(summary);

        html.Should().Contain("Mar 15, 2026");
    }

    [Fact]
    public void BuildDailySummaryHtml_ShowsDaysOverdue_ForOverdueItems()
    {
        var summary = CreateSampleSummary();
        var overdue = CreateSampleNotification(-5, isOverdue: true);
        summary.OverdueDeadlines.Add(overdue);

        var html = EmailNotificationService.BuildDailySummaryHtml(summary);

        html.Should().Contain("5d overdue");
    }

    [Fact]
    public void BuildDailySummaryHtml_ShowsDaysRemaining_ForUpcomingItems()
    {
        var summary = CreateSampleSummary();
        var upcoming = CreateSampleNotification(4);
        summary.UpcomingDeadlines.Add(upcoming);

        var html = EmailNotificationService.BuildDailySummaryHtml(summary);

        html.Should().Contain("4d remaining");
    }

    private static DeadlineNotification CreateSampleNotification(
        int daysUntilDue = -1, bool isOverdue = false, bool isAtRisk = false)
    {
        return new DeadlineNotification
        {
            DeadlineId = Guid.NewGuid(),
            MatterNumber = "MAT-001",
            MatterTitle = "Smith v. Smith",
            DeadlineTitle = "Response to Petition",
            DueDate = DateTime.UtcNow.Date.AddDays(daysUntilDue),
            DaysUntilDue = daysUntilDue,
            IsOverdue = isOverdue,
            IsAtRisk = isAtRisk,
        };
    }

    private static AttorneyDeadlineSummary CreateSampleSummary()
    {
        return new AttorneyDeadlineSummary
        {
            AttorneyId = Guid.NewGuid(),
            AttorneyName = "Jane Doe",
            AttorneyEmail = "jane@firm.com",
            FirmId = Guid.NewGuid(),
            FirmName = "Doe Family Law",
            GeneratedAtUtc = DateTime.UtcNow,
        };
    }
}
