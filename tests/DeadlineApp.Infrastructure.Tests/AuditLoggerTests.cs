using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;
using DeadlineApp.Core.Interfaces;
using DeadlineApp.Infrastructure.Data;
using DeadlineApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DeadlineApp.Infrastructure.Tests;

public class AuditLoggerTests : IDisposable
{
    private readonly DeadlineDbContext _context;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly AuditLogger _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly string _userEmail = "attorney@firm.com";

    public AuditLoggerTests()
    {
        var options = new DbContextOptionsBuilder<DeadlineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DeadlineDbContext(options);

        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockCurrentUser.Setup(u => u.UserId).Returns(_userId);
        _mockCurrentUser.Setup(u => u.Email).Returns(_userEmail);
        _mockCurrentUser.Setup(u => u.IpAddress).Returns("192.168.1.1");
        _mockCurrentUser.Setup(u => u.UserAgent).Returns("TestAgent/1.0");

        _sut = new AuditLogger(_context, _mockCurrentUser.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // --- LogAsync ---

    [Fact]
    public async Task LogAsync_CreatesAuditLogEntry()
    {
        var entityId = Guid.NewGuid();
        var matterId = Guid.NewGuid();

        await _sut.LogAsync(
            AuditAction.MatterCreated,
            nameof(Matter),
            entityId,
            matterId);

        var logs = await _context.AuditLogs.ToListAsync();
        logs.Should().HaveCount(1);
        var log = logs[0];
        log.Action.Should().Be(AuditAction.MatterCreated);
        log.EntityType.Should().Be(nameof(Matter));
        log.EntityId.Should().Be(entityId);
        log.MatterId.Should().Be(matterId);
        log.UserId.Should().Be(_userId);
        log.UserEmail.Should().Be(_userEmail);
        log.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task LogAsync_RecordsTimestamp()
    {
        var before = DateTime.UtcNow;

        await _sut.LogAsync(AuditAction.MatterAccessed, nameof(Matter), Guid.NewGuid());

        var log = await _context.AuditLogs.FirstAsync();
        log.Timestamp.Should().BeOnOrAfter(before);
        log.Timestamp.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task LogAsync_RecordsIpAndUserAgent()
    {
        await _sut.LogAsync(AuditAction.MatterAccessed, nameof(Matter), Guid.NewGuid());

        var log = await _context.AuditLogs.FirstAsync();
        log.IpAddress.Should().Be("192.168.1.1");
        log.UserAgent.Should().Be("TestAgent/1.0");
    }

    [Fact]
    public async Task LogAsync_SerializesNewValues()
    {
        await _sut.LogAsync(
            AuditAction.MatterCreated,
            nameof(Matter),
            Guid.NewGuid(),
            newValues: new { Title = "Smith v. Smith", State = "FL" });

        var log = await _context.AuditLogs.FirstAsync();
        log.NewValues.Should().NotBeNullOrEmpty();
        log.NewValues.Should().Contain("Smith v. Smith");
        log.NewValues.Should().Contain("FL");
    }

    [Fact]
    public async Task LogAsync_SerializesOldValues()
    {
        await _sut.LogAsync(
            AuditAction.MatterUpdated,
            nameof(Matter),
            Guid.NewGuid(),
            oldValues: new { Title = "Old Title" },
            newValues: new { Title = "New Title" });

        var log = await _context.AuditLogs.FirstAsync();
        log.OldValues.Should().Contain("Old Title");
        log.NewValues.Should().Contain("New Title");
    }

    [Fact]
    public async Task LogAsync_NullValues_StoresNull()
    {
        await _sut.LogAsync(AuditAction.MatterAccessed, nameof(Matter), Guid.NewGuid());

        var log = await _context.AuditLogs.FirstAsync();
        log.OldValues.Should().BeNull();
        log.NewValues.Should().BeNull();
    }

    // --- LogDeadlineCreatedAsync ---

    [Fact]
    public async Task LogDeadlineCreatedAsync_CreatesCorrectLogEntry()
    {
        var deadline = new Deadline
        {
            Id = Guid.NewGuid(),
            MatterId = Guid.NewGuid(),
            Title = "Response Due",
            DueDate = DateTime.UtcNow.AddDays(20),
            Type = DeadlineType.Court,
            CourtRuleId = Guid.NewGuid(),
            TriggerEventId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        await _sut.LogDeadlineCreatedAsync(deadline);

        var log = await _context.AuditLogs.FirstAsync();
        log.Action.Should().Be(AuditAction.DeadlineCreated);
        log.EntityType.Should().Be(nameof(Deadline));
        log.EntityId.Should().Be(deadline.Id);
        log.MatterId.Should().Be(deadline.MatterId);
        log.NewValues.Should().Contain("Response Due");
    }

    // --- LogDeadlineRecalculatedAsync ---

    [Fact]
    public async Task LogDeadlineRecalculatedAsync_RecordsOldAndNewDates()
    {
        var oldDueDate = new DateTime(2026, 2, 4);
        var deadline = new Deadline
        {
            Id = Guid.NewGuid(),
            MatterId = Guid.NewGuid(),
            Title = "Response Due",
            DueDate = new DateTime(2026, 2, 9),
            CalculatedDueDate = new DateTime(2026, 2, 9),
            CreatedAt = DateTime.UtcNow
        };

        await _sut.LogDeadlineRecalculatedAsync(deadline, oldDueDate);

        var log = await _context.AuditLogs.FirstAsync();
        log.Action.Should().Be(AuditAction.DeadlineRecalculated);
        log.OldValues.Should().Contain("2026-02-04");
        log.NewValues.Should().Contain("2026-02-09");
    }

    // --- LogDeadlineOverriddenAsync ---

    [Fact]
    public async Task LogDeadlineOverriddenAsync_RecordsOverrideDetails()
    {
        var originalDueDate = new DateTime(2026, 2, 4);
        var deadline = new Deadline
        {
            Id = Guid.NewGuid(),
            MatterId = Guid.NewGuid(),
            Title = "Response Due",
            DueDate = new DateTime(2026, 3, 1),
            OverrideApprovedBy = "partner@firm.com",
            CreatedAt = DateTime.UtcNow
        };

        await _sut.LogDeadlineOverriddenAsync(deadline, originalDueDate, "Judge extended deadline");

        var log = await _context.AuditLogs.FirstAsync();
        log.Action.Should().Be(AuditAction.DeadlineOverridden);
        log.NewValues.Should().Contain("Judge extended deadline");
    }

    // --- LogCalendarSyncAsync ---

    [Fact]
    public async Task LogCalendarSyncAsync_SuccessfulSync_LogsSuccessfully()
    {
        var link = new CalendarLink
        {
            Id = Guid.NewGuid(),
            DeadlineId = Guid.NewGuid(),
            UserId = _userId,
            OutlookEventId = "AAMkAGI2TG93AAA=",
            SyncStatus = SyncStatus.Synced,
            LastSyncedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _sut.LogCalendarSyncAsync(link, AuditAction.CalendarEventCreated);

        var log = await _context.AuditLogs.FirstAsync();
        log.Action.Should().Be(AuditAction.CalendarEventCreated);
        log.EntityType.Should().Be(nameof(CalendarLink));
        log.IsSuccess.Should().BeTrue();
        log.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task LogCalendarSyncAsync_FailedSync_LogsError()
    {
        var link = new CalendarLink
        {
            Id = Guid.NewGuid(),
            DeadlineId = Guid.NewGuid(),
            UserId = _userId,
            OutlookEventId = "",
            SyncStatus = SyncStatus.Failed,
            LastSyncedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _sut.LogCalendarSyncAsync(link, AuditAction.CalendarSyncFailed, "Graph API returned 503");

        var log = await _context.AuditLogs.FirstAsync();
        log.Action.Should().Be(AuditAction.CalendarSyncFailed);
        log.IsSuccess.Should().BeFalse();
        log.ErrorMessage.Should().Be("Graph API returned 503");
    }

    // --- LogMatterAccessAsync ---

    [Fact]
    public async Task LogMatterAccessAsync_LogsAccess()
    {
        var matterId = Guid.NewGuid();

        await _sut.LogMatterAccessAsync(matterId);

        var log = await _context.AuditLogs.FirstAsync();
        log.Action.Should().Be(AuditAction.MatterAccessed);
        log.MatterId.Should().Be(matterId);
        log.EntityType.Should().Be(nameof(Matter));
    }

    // --- Multiple log entries ---

    [Fact]
    public async Task MultipleLogCalls_CreatesMultipleEntries()
    {
        await _sut.LogAsync(AuditAction.MatterCreated, nameof(Matter), Guid.NewGuid());
        await _sut.LogAsync(AuditAction.DeadlineCreated, nameof(Deadline), Guid.NewGuid());
        await _sut.LogAsync(AuditAction.MatterAccessed, nameof(Matter), Guid.NewGuid());

        var count = await _context.AuditLogs.CountAsync();
        count.Should().Be(3);
    }

    [Fact]
    public async Task AuditLogs_AreOrderedByTimestamp()
    {
        await _sut.LogAsync(AuditAction.MatterCreated, nameof(Matter), Guid.NewGuid());
        await _sut.LogAsync(AuditAction.DeadlineCreated, nameof(Deadline), Guid.NewGuid());

        var logs = await _context.AuditLogs.OrderBy(l => l.Timestamp).ToListAsync();
        logs[0].Action.Should().Be(AuditAction.MatterCreated);
        logs[1].Action.Should().Be(AuditAction.DeadlineCreated);
        logs[1].Timestamp.Should().BeOnOrAfter(logs[0].Timestamp);
    }
}
