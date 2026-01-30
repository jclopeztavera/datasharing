using DeadlineApp.Api.Controllers;
using DeadlineApp.Api.DTOs;
using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;
using DeadlineApp.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DeadlineApp.Api.Tests.Controllers;

public class MattersControllerTests
{
    private readonly Mock<IMatterRepository> _mockMatterRepo;
    private readonly Mock<IDeadlineCalculator> _mockCalc;
    private readonly Mock<IAuditLogger> _mockAudit;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly MattersController _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _firmId = Guid.NewGuid();

    public MattersControllerTests()
    {
        _mockMatterRepo = new Mock<IMatterRepository>();
        _mockCalc = new Mock<IDeadlineCalculator>();
        _mockAudit = new Mock<IAuditLogger>();
        _mockCurrentUser = new Mock<ICurrentUserService>();

        _mockCurrentUser.Setup(u => u.UserId).Returns(_userId);
        _mockCurrentUser.Setup(u => u.FirmId).Returns(_firmId);
        _mockCurrentUser.Setup(u => u.Email).Returns("attorney@firm.com");
        _mockCurrentUser.Setup(u => u.IsAuthenticated).Returns(true);

        _sut = new MattersController(
            _mockMatterRepo.Object,
            _mockCalc.Object,
            _mockAudit.Object,
            _mockCurrentUser.Object);
    }

    private Matter CreateTestMatter(Guid? id = null)
    {
        return new Matter
        {
            Id = id ?? Guid.NewGuid(),
            FirmId = _firmId,
            MatterNumber = "FL-2026-001",
            Title = "Smith v. Smith",
            State = "FL",
            County = "Miami-Dade",
            CaseType = "Divorce",
            FilingDate = new DateTime(2026, 1, 15),
            ResponsibleAttorneyId = _userId,
            Status = MatterStatus.Active,
            CreatedAt = DateTime.UtcNow,
            Deadlines = new List<Deadline>(),
            Assignments = new List<MatterAssignment>()
        };
    }

    // --- GetMatters ---

    [Fact]
    public async Task GetMatters_ReturnsOkWithMatters()
    {
        var matters = new[] { CreateTestMatter(), CreateTestMatter() };
        _mockMatterRepo
            .Setup(r => r.GetByFirmAsync(_firmId, null, default))
            .ReturnsAsync(matters);

        var result = await _sut.GetMatters();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<MatterResponse>>().Subject;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMatters_NoFirmId_ReturnsForbid()
    {
        _mockCurrentUser.Setup(u => u.FirmId).Returns((Guid?)null);

        var result = await _sut.GetMatters();

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetMatters_WithStatusFilter_PassesFilterToRepository()
    {
        _mockMatterRepo
            .Setup(r => r.GetByFirmAsync(_firmId, MatterStatus.Active, default))
            .ReturnsAsync(Array.Empty<Matter>());

        await _sut.GetMatters(MatterStatus.Active);

        _mockMatterRepo.Verify(r => r.GetByFirmAsync(_firmId, MatterStatus.Active, default), Times.Once);
    }

    // --- GetMatter ---

    [Fact]
    public async Task GetMatter_ValidAccess_ReturnsOk()
    {
        var matter = CreateTestMatter();
        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(matter.Id, _userId, default)).ReturnsAsync(true);
        _mockMatterRepo.Setup(r => r.GetByIdWithDeadlinesAsync(matter.Id, default)).ReturnsAsync(matter);

        var result = await _sut.GetMatter(matter.Id);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMatter_NoAccess_ReturnsForbid()
    {
        var matterId = Guid.NewGuid();
        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(matterId, _userId, default)).ReturnsAsync(false);

        var result = await _sut.GetMatter(matterId);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetMatter_NotFound_ReturnsNotFound()
    {
        var matterId = Guid.NewGuid();
        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(matterId, _userId, default)).ReturnsAsync(true);
        _mockMatterRepo.Setup(r => r.GetByIdWithDeadlinesAsync(matterId, default)).ReturnsAsync((Matter?)null);

        var result = await _sut.GetMatter(matterId);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetMatter_LogsMatterAccess()
    {
        var matter = CreateTestMatter();
        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(matter.Id, _userId, default)).ReturnsAsync(true);
        _mockMatterRepo.Setup(r => r.GetByIdWithDeadlinesAsync(matter.Id, default)).ReturnsAsync(matter);

        await _sut.GetMatter(matter.Id);

        _mockAudit.Verify(a => a.LogMatterAccessAsync(matter.Id, default), Times.Once);
    }

    [Fact]
    public async Task GetMatter_NoUserId_ReturnsForbid()
    {
        _mockCurrentUser.Setup(u => u.UserId).Returns((Guid?)null);

        var result = await _sut.GetMatter(Guid.NewGuid());

        result.Result.Should().BeOfType<ForbidResult>();
    }

    // --- CreateMatter ---

    [Fact]
    public async Task CreateMatter_ValidRequest_ReturnsCreatedAtAction()
    {
        var request = new CreateMatterRequest(
            MatterNumber: "FL-2026-002",
            Title: "Jones v. Jones",
            State: "FL",
            County: "Broward",
            CaseType: "Custody",
            FilingDate: DateTime.UtcNow.AddDays(-5),
            ResponsibleAttorneyId: _userId);

        _mockMatterRepo
            .Setup(r => r.CreateAsync(It.IsAny<Matter>(), default))
            .ReturnsAsync((Matter m, CancellationToken _) => m);

        var result = await _sut.CreateMatter(request);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var created = (CreatedAtActionResult)result.Result!;
        var response = created.Value.Should().BeOfType<MatterResponse>().Subject;
        response.MatterNumber.Should().Be("FL-2026-002");
        response.Status.Should().Be(MatterStatus.Active);
    }

    [Fact]
    public async Task CreateMatter_LogsCreation()
    {
        var request = new CreateMatterRequest("FL-2026-002", "Jones v. Jones", "FL", "Broward", "Custody", DateTime.UtcNow.AddDays(-5), _userId);

        _mockMatterRepo
            .Setup(r => r.CreateAsync(It.IsAny<Matter>(), default))
            .ReturnsAsync((Matter m, CancellationToken _) => m);

        await _sut.CreateMatter(request);

        _mockAudit.Verify(a => a.LogAsync(
            AuditAction.MatterCreated,
            nameof(Matter),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            null,
            It.IsAny<object>(),
            default), Times.Once);
    }

    [Fact]
    public async Task CreateMatter_NoFirmId_ReturnsForbid()
    {
        _mockCurrentUser.Setup(u => u.FirmId).Returns((Guid?)null);
        var request = new CreateMatterRequest("FL-2026-002", "Jones v. Jones", "FL", "Broward", "Custody", DateTime.UtcNow.AddDays(-5), _userId);

        var result = await _sut.CreateMatter(request);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    // --- UpdateMatter ---

    [Fact]
    public async Task UpdateMatter_ValidRequest_ReturnsOk()
    {
        var matter = CreateTestMatter();
        var request = new UpdateMatterRequest("Updated Title", "FL", "Palm Beach", "Divorce", DateTime.UtcNow.AddDays(-5), _userId, MatterStatus.Active);

        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(matter.Id, _userId, default)).ReturnsAsync(true);
        _mockMatterRepo.Setup(r => r.GetByIdAsync(matter.Id, default)).ReturnsAsync(matter);
        _mockMatterRepo.Setup(r => r.UpdateAsync(It.IsAny<Matter>(), default)).ReturnsAsync((Matter m, CancellationToken _) => m);

        var result = await _sut.UpdateMatter(matter.Id, request);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<MatterResponse>().Subject;
        response.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdateMatter_NoAccess_ReturnsForbid()
    {
        var matterId = Guid.NewGuid();
        var request = new UpdateMatterRequest("Title", "FL", "County", "Divorce", DateTime.UtcNow, _userId, MatterStatus.Active);
        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(matterId, _userId, default)).ReturnsAsync(false);

        var result = await _sut.UpdateMatter(matterId, request);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateMatter_NotFound_ReturnsNotFound()
    {
        var matterId = Guid.NewGuid();
        var request = new UpdateMatterRequest("Title", "FL", "County", "Divorce", DateTime.UtcNow, _userId, MatterStatus.Active);
        _mockMatterRepo.Setup(r => r.UserHasAccessAsync(matterId, _userId, default)).ReturnsAsync(true);
        _mockMatterRepo.Setup(r => r.GetByIdAsync(matterId, default)).ReturnsAsync((Matter?)null);

        var result = await _sut.UpdateMatter(matterId, request);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // --- GetMyMatters ---

    [Fact]
    public async Task GetMyMatters_ReturnsAssignedMatters()
    {
        var matters = new[] { CreateTestMatter() };
        _mockMatterRepo.Setup(r => r.GetByUserAssignmentAsync(_userId, default)).ReturnsAsync(matters);

        var result = await _sut.GetMyMatters();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<MatterResponse>>().Subject;
        response.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMyMatters_NoUserId_ReturnsForbid()
    {
        _mockCurrentUser.Setup(u => u.UserId).Returns((Guid?)null);

        var result = await _sut.GetMyMatters();

        result.Result.Should().BeOfType<ForbidResult>();
    }
}
