using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;
using DeadlineApp.Core.Interfaces;
using DeadlineApp.Core.Models;
using DeadlineApp.Functions;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeadlineApp.Infrastructure.Tests;

public class DeadlineNotificationFunctionTests
{
    private readonly Mock<IDeadlineRepository> _repoMock;
    private readonly Mock<INotificationService> _emailServiceMock;
    private readonly Mock<INotificationService> _teamsServiceMock;
    private readonly Mock<IAuditLogger> _auditMock;
    private readonly Mock<ILogger<DeadlineNotificationFunction>> _loggerMock;
    private readonly DeadlineNotificationFunction _function;

    public DeadlineNotificationFunctionTests()
    {
        _repoMock = new Mock<IDeadlineRepository>();
        _emailServiceMock = new Mock<INotificationService>();
        _teamsServiceMock = new Mock<INotificationService>();
        _auditMock = new Mock<IAuditLogger>();
        _loggerMock = new Mock<ILogger<DeadlineNotificationFunction>>();

        var services = new List<INotificationService>
        {
            _emailServiceMock.Object,
            _teamsServiceMock.Object
        };

        _function = new DeadlineNotificationFunction(
            _repoMock.Object,
            services,
            _auditMock.Object,
            _loggerMock.Object);
    }

    // ---- CheckOverdueDeadlines tests ----

    [Fact]
    public async Task CheckOverdueDeadlines_NoOverdue_DoesNotSendNotifications()
    {
        _repoMock.Setup(r => r.GetAllOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Deadline>());

        await _function.CheckOverdueDeadlines(CreateTimerInfo(), CancellationToken.None);

        _emailServiceMock.Verify(
            s => s.SendOverdueAlertAsync(
                It.IsAny<DeadlineNotification>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckOverdueDeadlines_MarksDeadlinesAsOverdue()
    {
        var deadline = CreateOverdueDeadline(DeadlineStatus.Pending);

        _repoMock.Setup(r => r.GetAllOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { deadline });
        SetupNotificationSuccess();

        await _function.CheckOverdueDeadlines(CreateTimerInfo(), CancellationToken.None);

        deadline.Status.Should().Be(DeadlineStatus.Overdue);
        _repoMock.Verify(r => r.UpdateAsync(deadline, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckOverdueDeadlines_SendsNotificationToAllServices()
    {
        var deadline = CreateOverdueDeadline(DeadlineStatus.Pending);

        _repoMock.Setup(r => r.GetAllOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { deadline });
        SetupNotificationSuccess();

        await _function.CheckOverdueDeadlines(CreateTimerInfo(), CancellationToken.None);

        _emailServiceMock.Verify(
            s => s.SendOverdueAlertAsync(
                It.Is<DeadlineNotification>(n => n.DeadlineId == deadline.Id),
                "jane@firm.com", "Jane Doe", It.IsAny<CancellationToken>()),
            Times.Once);

        _teamsServiceMock.Verify(
            s => s.SendOverdueAlertAsync(
                It.Is<DeadlineNotification>(n => n.DeadlineId == deadline.Id),
                "jane@firm.com", "Jane Doe", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckOverdueDeadlines_LogsAuditForMarkedOverdue()
    {
        var deadline = CreateOverdueDeadline(DeadlineStatus.Pending);

        _repoMock.Setup(r => r.GetAllOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { deadline });
        SetupNotificationSuccess();

        await _function.CheckOverdueDeadlines(CreateTimerInfo(), CancellationToken.None);

        _auditMock.Verify(a => a.LogAsync(
            AuditAction.DeadlineMarkedOverdue,
            "Deadline", deadline.Id, deadline.MatterId,
            It.IsAny<object>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckOverdueDeadlines_LogsAuditForNotificationSent()
    {
        var deadline = CreateOverdueDeadline(DeadlineStatus.Pending);

        _repoMock.Setup(r => r.GetAllOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { deadline });
        SetupNotificationSuccess();

        await _function.CheckOverdueDeadlines(CreateTimerInfo(), CancellationToken.None);

        _auditMock.Verify(a => a.LogAsync(
            AuditAction.NotificationSent,
            "Deadline", deadline.Id, deadline.MatterId,
            It.IsAny<object>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)); // email + teams
    }

    [Fact]
    public async Task CheckOverdueDeadlines_LogsAuditForNotificationFailed()
    {
        var deadline = CreateOverdueDeadline(DeadlineStatus.Pending);

        _repoMock.Setup(r => r.GetAllOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { deadline });

        _emailServiceMock.Setup(s => s.SendOverdueAlertAsync(
                It.IsAny<DeadlineNotification>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationResult.Failed("email", "SMTP error"));

        _teamsServiceMock.Setup(s => s.SendOverdueAlertAsync(
                It.IsAny<DeadlineNotification>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationResult.Succeeded("teams"));

        await _function.CheckOverdueDeadlines(CreateTimerInfo(), CancellationToken.None);

        _auditMock.Verify(a => a.LogAsync(
            AuditAction.NotificationFailed,
            "Deadline", deadline.Id, deadline.MatterId,
            It.IsAny<object>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _auditMock.Verify(a => a.LogAsync(
            AuditAction.NotificationSent,
            "Deadline", deadline.Id, deadline.MatterId,
            It.IsAny<object>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckOverdueDeadlines_SkipsInactiveAttorneys()
    {
        var deadline = CreateOverdueDeadline(DeadlineStatus.Pending);
        deadline.Matter.ResponsibleAttorney.IsActive = false;

        _repoMock.Setup(r => r.GetAllOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { deadline });

        await _function.CheckOverdueDeadlines(CreateTimerInfo(), CancellationToken.None);

        // Should still mark overdue, but not send notifications
        deadline.Status.Should().Be(DeadlineStatus.Overdue);
        _emailServiceMock.Verify(
            s => s.SendOverdueAlertAsync(
                It.IsAny<DeadlineNotification>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckOverdueDeadlines_HandlesMultipleDeadlines()
    {
        var d1 = CreateOverdueDeadline(DeadlineStatus.Pending);
        var d2 = CreateOverdueDeadline(DeadlineStatus.Pending);
        d2.Id = Guid.NewGuid();

        _repoMock.Setup(r => r.GetAllOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { d1, d2 });
        SetupNotificationSuccess();

        await _function.CheckOverdueDeadlines(CreateTimerInfo(), CancellationToken.None);

        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Deadline>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _emailServiceMock.Verify(
            s => s.SendOverdueAlertAsync(
                It.IsAny<DeadlineNotification>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    // ---- SendDailyDeadlineSummary tests ----

    [Fact]
    public async Task SendDailySummary_NoDeadlines_DoesNotSendAnything()
    {
        SetupEmptyRepositories();

        await _function.SendDailyDeadlineSummary(CreateTimerInfo(), CancellationToken.None);

        _emailServiceMock.Verify(
            s => s.SendDailySummaryAsync(It.IsAny<AttorneyDeadlineSummary>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendDailySummary_GroupsByAttorney()
    {
        var attorney1Id = Guid.NewGuid();
        var attorney2Id = Guid.NewGuid();
        var firm = CreateFirm();
        var attorney1 = CreateAttorney(attorney1Id, "Alice", "alice@firm.com", firm);
        var attorney2 = CreateAttorney(attorney2Id, "Bob", "bob@firm.com", firm);

        var d1 = CreateDeadlineForAttorney(attorney1, firm, -1);
        var d2 = CreateDeadlineForAttorney(attorney2, firm, -2);
        var d3 = CreateDeadlineForAttorney(attorney1, firm, 3);

        _repoMock.Setup(r => r.GetAllOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { d1, d2 });
        _repoMock.Setup(r => r.GetAllAtRiskBufferAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Deadline>());
        _repoMock.Setup(r => r.GetAllUpcomingGroupedByAttorneyAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { d3 });

        SetupDailySummarySuccess();

        await _function.SendDailyDeadlineSummary(CreateTimerInfo(), CancellationToken.None);

        // Should send to both attorneys (2 services × 2 attorneys = 4 calls)
        _emailServiceMock.Verify(
            s => s.SendDailySummaryAsync(
                It.Is<AttorneyDeadlineSummary>(sum => sum.AttorneyEmail == "alice@firm.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _emailServiceMock.Verify(
            s => s.SendDailySummaryAsync(
                It.Is<AttorneyDeadlineSummary>(sum => sum.AttorneyEmail == "bob@firm.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendDailySummary_CategorizesDeadlinesCorrectly()
    {
        var firm = CreateFirm();
        var attorney = CreateAttorney(Guid.NewGuid(), "Jane", "jane@firm.com", firm);

        var overdue = CreateDeadlineForAttorney(attorney, firm, -2);
        var atRisk = CreateDeadlineForAttorney(attorney, firm, 1);
        atRisk.Type = DeadlineType.Buffer;
        var upcoming = CreateDeadlineForAttorney(attorney, firm, 5);

        _repoMock.Setup(r => r.GetAllOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { overdue });
        _repoMock.Setup(r => r.GetAllAtRiskBufferAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { atRisk });
        _repoMock.Setup(r => r.GetAllUpcomingGroupedByAttorneyAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { upcoming });

        AttorneyDeadlineSummary? capturedSummary = null;
        _emailServiceMock.Setup(s => s.SendDailySummaryAsync(
                It.IsAny<AttorneyDeadlineSummary>(), It.IsAny<CancellationToken>()))
            .Callback<AttorneyDeadlineSummary, CancellationToken>((s, _) => capturedSummary = s)
            .ReturnsAsync(NotificationResult.Succeeded("email"));
        _teamsServiceMock.Setup(s => s.SendDailySummaryAsync(
                It.IsAny<AttorneyDeadlineSummary>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationResult.Succeeded("teams"));

        await _function.SendDailyDeadlineSummary(CreateTimerInfo(), CancellationToken.None);

        capturedSummary.Should().NotBeNull();
        capturedSummary!.OverdueDeadlines.Should().HaveCount(1);
        capturedSummary.AtRiskDeadlines.Should().HaveCount(1);
        capturedSummary.UpcomingDeadlines.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendDailySummary_LogsAuditForEachSummary()
    {
        var firm = CreateFirm();
        var attorney = CreateAttorney(Guid.NewGuid(), "Jane", "jane@firm.com", firm);
        var overdue = CreateDeadlineForAttorney(attorney, firm, -1);

        _repoMock.Setup(r => r.GetAllOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { overdue });
        _repoMock.Setup(r => r.GetAllAtRiskBufferAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Deadline>());
        _repoMock.Setup(r => r.GetAllUpcomingGroupedByAttorneyAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Deadline>());

        SetupDailySummarySuccess();

        await _function.SendDailyDeadlineSummary(CreateTimerInfo(), CancellationToken.None);

        _auditMock.Verify(a => a.LogAsync(
            AuditAction.DailySummarySent,
            "User", attorney.Id, null,
            It.IsAny<object>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)); // email + teams
    }

    [Fact]
    public async Task SendDailySummary_SkipsInactiveAttorneys()
    {
        var firm = CreateFirm();
        var attorney = CreateAttorney(Guid.NewGuid(), "Jane", "jane@firm.com", firm);
        attorney.IsActive = false;

        var overdue = CreateDeadlineForAttorney(attorney, firm, -1);

        _repoMock.Setup(r => r.GetAllOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { overdue });
        _repoMock.Setup(r => r.GetAllAtRiskBufferAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Deadline>());
        _repoMock.Setup(r => r.GetAllUpcomingGroupedByAttorneyAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Deadline>());

        await _function.SendDailyDeadlineSummary(CreateTimerInfo(), CancellationToken.None);

        _emailServiceMock.Verify(
            s => s.SendDailySummaryAsync(It.IsAny<AttorneyDeadlineSummary>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---- NotificationResult tests ----

    [Fact]
    public void NotificationResult_Succeeded_SetsCorrectValues()
    {
        var result = NotificationResult.Succeeded("email", "msg-123");

        result.Success.Should().BeTrue();
        result.Channel.Should().Be("email");
        result.MessageId.Should().Be("msg-123");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void NotificationResult_Failed_SetsCorrectValues()
    {
        var result = NotificationResult.Failed("teams", "Connection timeout");

        result.Success.Should().BeFalse();
        result.Channel.Should().Be("teams");
        result.ErrorMessage.Should().Be("Connection timeout");
    }

    // ---- Helpers ----

    private void SetupNotificationSuccess()
    {
        _emailServiceMock.Setup(s => s.SendOverdueAlertAsync(
                It.IsAny<DeadlineNotification>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationResult.Succeeded("email"));

        _teamsServiceMock.Setup(s => s.SendOverdueAlertAsync(
                It.IsAny<DeadlineNotification>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationResult.Succeeded("teams"));
    }

    private void SetupDailySummarySuccess()
    {
        _emailServiceMock.Setup(s => s.SendDailySummaryAsync(
                It.IsAny<AttorneyDeadlineSummary>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationResult.Succeeded("email"));

        _teamsServiceMock.Setup(s => s.SendDailySummaryAsync(
                It.IsAny<AttorneyDeadlineSummary>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationResult.Succeeded("teams"));
    }

    private void SetupEmptyRepositories()
    {
        _repoMock.Setup(r => r.GetAllOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Deadline>());
        _repoMock.Setup(r => r.GetAllAtRiskBufferAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Deadline>());
        _repoMock.Setup(r => r.GetAllUpcomingGroupedByAttorneyAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Deadline>());
    }

    private static Deadline CreateOverdueDeadline(DeadlineStatus status)
    {
        var firmId = Guid.NewGuid();
        var attorneyId = Guid.NewGuid();
        var matterId = Guid.NewGuid();

        return new Deadline
        {
            Id = Guid.NewGuid(),
            MatterId = matterId,
            Title = "Response to Petition",
            DueDate = DateTime.UtcNow.Date.AddDays(-3),
            CalculatedDueDate = DateTime.UtcNow.Date.AddDays(-3),
            Status = status,
            Type = DeadlineType.Court,
            Matter = new Matter
            {
                Id = matterId,
                MatterNumber = "MAT-001",
                Title = "Smith v. Smith",
                State = "FL",
                County = "Miami-Dade",
                CaseType = "Divorce",
                FilingDate = DateTime.UtcNow.Date.AddDays(-30),
                FirmId = firmId,
                ResponsibleAttorneyId = attorneyId,
                ResponsibleAttorney = new User
                {
                    Id = attorneyId,
                    Email = "jane@firm.com",
                    DisplayName = "Jane Doe",
                    EntraObjectId = "entra-123",
                    Role = UserRole.Attorney,
                    IsActive = true,
                    FirmId = firmId,
                },
                Firm = new Firm { Id = firmId, Name = "Doe Family Law", TenantId = "tenant-1" },
            }
        };
    }

    private static Firm CreateFirm()
    {
        return new Firm
        {
            Id = Guid.NewGuid(),
            Name = "Test Family Law",
            TenantId = "tenant-" + Guid.NewGuid().ToString()[..8],
        };
    }

    private static User CreateAttorney(Guid id, string name, string email, Firm firm)
    {
        return new User
        {
            Id = id,
            DisplayName = name,
            Email = email,
            EntraObjectId = "entra-" + id.ToString()[..8],
            Role = UserRole.Attorney,
            IsActive = true,
            FirmId = firm.Id,
            Firm = firm,
        };
    }

    private static Deadline CreateDeadlineForAttorney(User attorney, Firm firm, int daysFromToday)
    {
        var matterId = Guid.NewGuid();
        return new Deadline
        {
            Id = Guid.NewGuid(),
            MatterId = matterId,
            Title = $"Test Deadline ({daysFromToday}d)",
            DueDate = DateTime.UtcNow.Date.AddDays(daysFromToday),
            CalculatedDueDate = DateTime.UtcNow.Date.AddDays(daysFromToday),
            Status = DeadlineStatus.Pending,
            Type = DeadlineType.Court,
            Matter = new Matter
            {
                Id = matterId,
                MatterNumber = "MAT-" + matterId.ToString()[..4],
                Title = "Test Matter",
                State = "FL",
                County = "Miami-Dade",
                CaseType = "Divorce",
                FilingDate = DateTime.UtcNow.Date.AddDays(-60),
                FirmId = firm.Id,
                Firm = firm,
                ResponsibleAttorneyId = attorney.Id,
                ResponsibleAttorney = attorney,
            }
        };
    }

    private static TimerInfo CreateTimerInfo()
    {
        return new Mock<TimerInfo>().Object;
    }
}
