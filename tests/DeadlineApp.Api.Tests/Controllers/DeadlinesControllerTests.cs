using DeadlineApp.Api.Controllers;
using DeadlineApp.Api.DTOs;
using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;
using DeadlineApp.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DeadlineApp.Api.Tests.Controllers;

public class DeadlinesControllerTests
{
    private readonly Mock<IDeadlineRepository> _mockDeadlineRepo;
    private readonly Mock<IMatterRepository> _mockMatterRepo;
    private readonly Mock<IDeadlineCalculator> _mockCalc;
    private readonly Mock<IAuditLogger> _mockAudit;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly DeadlinesController _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _firmId = Guid.NewGuid();

    public DeadlinesControllerTests()
    {
        _mockDeadlineRepo = new Mock<IDeadlineRepository>();
        _mockMatterRepo = new Mock<IMatterRepository>();
        _mockCalc = new Mock<IDeadlineCalculator>();
        _mockAudit = new Mock<IAuditLogger>();
        _mockCurrentUser = new Mock<ICurrentUserService>();

        _mockCurrentUser.Setup(u => u.UserId).Returns(_userId);
        _mockCurrentUser.Setup(u => u.FirmId).Returns(_firmId);
        _mockCurrentUser.Setup(u => u.Email).Returns("attorney@firm.com");

        _sut = new DeadlinesController(
            _mockDeadlineRepo.Object,
            _mockMatterRepo.Object,
            _mockCalc.Object,
            _mockAudit.Object,
            _mockCurrentUser.Object);
    }

    private Deadline CreateTestDeadline(Guid? matterId = null)
    {
        return new Deadline
        {
            Id = Guid.NewGuid(),
            MatterId = matterId ?? Guid.NewGuid(),
            Title = "Response Deadline",
            Description = "File response to petition",
            DueDate = DateTime.UtcNow.AddDays(20),
            CalculatedDueDate = DateTime.UtcNow.AddDays(20),
            Type = DeadlineType.Court,
            Status = DeadlineStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    // --- GetByMatter ---

    [Fact]
    public async Task GetByMatter_WithAccess_ReturnsDeadlines()
    {
        var matterId = Guid.NewGuid();
        var deadlines = new[] { CreateTestDeadline(matterId), CreateTestDeadline(matterId) };

        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(matterId, _userId, default)).ReturnsAsync(true);
        _mockDeadlineRepo.Setup(r => r.GetByMatterAsync(matterId, default)).ReturnsAsync(deadlines);

        var result = await _sut.GetByMatter(matterId);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<DeadlineResponse>>().Subject;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByMatter_NoAccess_ReturnsForbid()
    {
        var matterId = Guid.NewGuid();
        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(matterId, _userId, default)).ReturnsAsync(false);

        var result = await _sut.GetByMatter(matterId);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    // --- GetDeadline ---

    [Fact]
    public async Task GetDeadline_Found_ReturnsOk()
    {
        var deadline = CreateTestDeadline();
        _mockDeadlineRepo.Setup(r => r.GetByIdAsync(deadline.Id, default)).ReturnsAsync(deadline);
        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(deadline.MatterId, _userId, default)).ReturnsAsync(true);

        var result = await _sut.GetDeadline(deadline.Id);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDeadline_NotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _mockDeadlineRepo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Deadline?)null);

        var result = await _sut.GetDeadline(id);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetDeadline_NoMatterAccess_ReturnsForbid()
    {
        var deadline = CreateTestDeadline();
        _mockDeadlineRepo.Setup(r => r.GetByIdAsync(deadline.Id, default)).ReturnsAsync(deadline);
        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(deadline.MatterId, _userId, default)).ReturnsAsync(false);

        var result = await _sut.GetDeadline(deadline.Id);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    // --- CreateDeadline ---

    [Fact]
    public async Task CreateDeadline_ValidRequest_ReturnsCreatedAtAction()
    {
        var matterId = Guid.NewGuid();
        var request = new CreateDeadlineRequest(matterId, "New Deadline", "Description", DateTime.UtcNow.AddDays(30), DeadlineType.Court);

        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(matterId, _userId, default)).ReturnsAsync(true);
        _mockDeadlineRepo.Setup(r => r.CreateAsync(It.IsAny<Deadline>(), default))
            .ReturnsAsync((Deadline d, CancellationToken _) => d);

        var result = await _sut.CreateDeadline(request);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateDeadline_SetsManualOverrideTrue()
    {
        var matterId = Guid.NewGuid();
        var request = new CreateDeadlineRequest(matterId, "Manual Deadline", null, DateTime.UtcNow.AddDays(30), DeadlineType.Court);

        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(matterId, _userId, default)).ReturnsAsync(true);
        _mockDeadlineRepo.Setup(r => r.CreateAsync(It.IsAny<Deadline>(), default))
            .ReturnsAsync((Deadline d, CancellationToken _) => d);

        var result = await _sut.CreateDeadline(request);

        var created = (CreatedAtActionResult)result.Result!;
        var response = created.Value.Should().BeOfType<DeadlineResponse>().Subject;
        response.IsManualOverride.Should().BeTrue();
    }

    [Fact]
    public async Task CreateDeadline_LogsCreation()
    {
        var matterId = Guid.NewGuid();
        var request = new CreateDeadlineRequest(matterId, "Deadline", null, DateTime.UtcNow.AddDays(30), DeadlineType.Court);

        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(matterId, _userId, default)).ReturnsAsync(true);
        _mockDeadlineRepo.Setup(r => r.CreateAsync(It.IsAny<Deadline>(), default))
            .ReturnsAsync((Deadline d, CancellationToken _) => d);

        await _sut.CreateDeadline(request);

        _mockAudit.Verify(a => a.LogDeadlineCreatedAsync(It.IsAny<Deadline>(), default), Times.Once);
    }

    // --- OverrideDeadline ---

    [Fact]
    public async Task OverrideDeadline_ValidRequest_ReturnsOkWithUpdatedDeadline()
    {
        var deadline = CreateTestDeadline();
        var originalDueDate = deadline.DueDate;
        var newDueDate = DateTime.UtcNow.AddDays(40);
        var request = new OverrideDeadlineRequest(newDueDate, "Judge granted extension at January hearing");

        _mockDeadlineRepo.Setup(r => r.GetByIdAsync(deadline.Id, default)).ReturnsAsync(deadline);
        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(deadline.MatterId, _userId, default)).ReturnsAsync(true);
        _mockDeadlineRepo.Setup(r => r.UpdateAsync(It.IsAny<Deadline>(), default))
            .ReturnsAsync((Deadline d, CancellationToken _) => d);

        var result = await _sut.OverrideDeadline(deadline.Id, request);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<DeadlineResponse>().Subject;
        response.DueDate.Should().Be(newDueDate);
        response.IsManualOverride.Should().BeTrue();
        response.OverrideReason.Should().Be("Judge granted extension at January hearing");
    }

    [Fact]
    public async Task OverrideDeadline_LogsOverride()
    {
        var deadline = CreateTestDeadline();
        var request = new OverrideDeadlineRequest(DateTime.UtcNow.AddDays(40), "Judge granted extension at January hearing");

        _mockDeadlineRepo.Setup(r => r.GetByIdAsync(deadline.Id, default)).ReturnsAsync(deadline);
        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(deadline.MatterId, _userId, default)).ReturnsAsync(true);
        _mockDeadlineRepo.Setup(r => r.UpdateAsync(It.IsAny<Deadline>(), default))
            .ReturnsAsync((Deadline d, CancellationToken _) => d);

        await _sut.OverrideDeadline(deadline.Id, request);

        _mockAudit.Verify(a => a.LogDeadlineOverriddenAsync(
            It.IsAny<Deadline>(),
            It.IsAny<DateTime>(),
            "Judge granted extension at January hearing",
            default), Times.Once);
    }

    [Fact]
    public async Task OverrideDeadline_NotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _mockDeadlineRepo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Deadline?)null);
        var request = new OverrideDeadlineRequest(DateTime.UtcNow.AddDays(40), "Reason for the override");

        var result = await _sut.OverrideDeadline(id, request);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // --- CompleteDeadline ---

    [Fact]
    public async Task CompleteDeadline_SetsCompletedStatus()
    {
        var deadline = CreateTestDeadline();
        _mockDeadlineRepo.Setup(r => r.GetByIdAsync(deadline.Id, default)).ReturnsAsync(deadline);
        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(deadline.MatterId, _userId, default)).ReturnsAsync(true);
        _mockDeadlineRepo.Setup(r => r.UpdateAsync(It.IsAny<Deadline>(), default))
            .ReturnsAsync((Deadline d, CancellationToken _) => d);

        var result = await _sut.CompleteDeadline(deadline.Id);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<DeadlineResponse>().Subject;
        response.Status.Should().Be(DeadlineStatus.Completed);
    }

    [Fact]
    public async Task CompleteDeadline_SetsCompletedByAndCompletedAt()
    {
        var deadline = CreateTestDeadline();
        _mockDeadlineRepo.Setup(r => r.GetByIdAsync(deadline.Id, default)).ReturnsAsync(deadline);
        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(deadline.MatterId, _userId, default)).ReturnsAsync(true);
        _mockDeadlineRepo.Setup(r => r.UpdateAsync(It.IsAny<Deadline>(), default))
            .ReturnsAsync((Deadline d, CancellationToken _) => d);

        await _sut.CompleteDeadline(deadline.Id);

        deadline.CompletedBy.Should().Be("attorney@firm.com");
        deadline.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteDeadline_LogsCompletion()
    {
        var deadline = CreateTestDeadline();
        _mockDeadlineRepo.Setup(r => r.GetByIdAsync(deadline.Id, default)).ReturnsAsync(deadline);
        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(deadline.MatterId, _userId, default)).ReturnsAsync(true);
        _mockDeadlineRepo.Setup(r => r.UpdateAsync(It.IsAny<Deadline>(), default))
            .ReturnsAsync((Deadline d, CancellationToken _) => d);

        await _sut.CompleteDeadline(deadline.Id);

        _mockAudit.Verify(a => a.LogAsync(
            AuditAction.DeadlineCompleted,
            nameof(Deadline),
            deadline.Id,
            deadline.MatterId,
            null,
            It.IsAny<object>(),
            default), Times.Once);
    }

    [Fact]
    public async Task CompleteDeadline_NotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _mockDeadlineRepo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Deadline?)null);

        var result = await _sut.CompleteDeadline(id);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CompleteDeadline_NoAccess_ReturnsForbid()
    {
        var deadline = CreateTestDeadline();
        _mockDeadlineRepo.Setup(r => r.GetByIdAsync(deadline.Id, default)).ReturnsAsync(deadline);
        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(deadline.MatterId, _userId, default)).ReturnsAsync(false);

        var result = await _sut.CompleteDeadline(deadline.Id);

        result.Result.Should().BeOfType<ForbidResult>();
    }
}
