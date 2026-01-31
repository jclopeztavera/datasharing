using DeadlineApp.Core.Enums;
using DeadlineApp.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DeadlineApp.Infrastructure.Tests;

public class CourtRuleSeedDataTests : IDisposable
{
    private readonly DeadlineDbContext _context;

    public CourtRuleSeedDataTests()
    {
        var options = new DbContextOptionsBuilder<DeadlineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DeadlineDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // --- GetAllRules ---

    [Fact]
    public void GetAllRules_ReturnsNonEmpty()
    {
        var rules = CourtRuleSeedData.GetAllRules();
        rules.Should().NotBeEmpty();
    }

    [Fact]
    public void GetAllRules_AllHaveUniqueIds()
    {
        var rules = CourtRuleSeedData.GetAllRules();
        rules.Select(r => r.Id).Distinct().Should().HaveCount(rules.Count);
    }

    [Fact]
    public void GetAllRules_AllHaveRequiredFields()
    {
        var rules = CourtRuleSeedData.GetAllRules();

        foreach (var rule in rules)
        {
            rule.Id.Should().NotBe(Guid.Empty);
            rule.State.Should().NotBeNullOrEmpty();
            rule.CaseType.Should().NotBeNullOrEmpty();
            rule.RuleName.Should().NotBeNullOrEmpty();
            rule.RuleDescription.Should().NotBeNullOrEmpty();
            rule.RuleCitation.Should().NotBeNullOrEmpty();
            rule.DaysFromTrigger.Should().BeGreaterThan(0);
            rule.IsActive.Should().BeTrue();
            rule.CreatedBy.Should().Be("system-seed");
        }
    }

    // --- State coverage ---

    [Fact]
    public void GetAllRules_ContainsBothStates()
    {
        var rules = CourtRuleSeedData.GetAllRules();
        var states = rules.Select(r => r.State).Distinct().ToList();

        states.Should().Contain("FL");
        states.Should().Contain("NY");
    }

    [Fact]
    public void GetFloridaRules_OnlyContainsFL()
    {
        var rules = CourtRuleSeedData.GetFloridaRules();
        rules.Should().OnlyContain(r => r.State == "FL");
    }

    [Fact]
    public void GetNewYorkRules_OnlyContainsNY()
    {
        var rules = CourtRuleSeedData.GetNewYorkRules();
        rules.Should().OnlyContain(r => r.State == "NY");
    }

    // --- Case type coverage ---

    [Theory]
    [InlineData("FL", "Divorce")]
    [InlineData("FL", "Custody")]
    [InlineData("FL", "Child Support")]
    [InlineData("FL", "Alimony")]
    [InlineData("FL", "Paternity")]
    [InlineData("FL", "Adoption")]
    [InlineData("NY", "Divorce")]
    [InlineData("NY", "Custody")]
    [InlineData("NY", "Child Support")]
    [InlineData("NY", "Alimony")]
    [InlineData("NY", "Paternity")]
    [InlineData("NY", "Adoption")]
    public void GetAllRules_ContainsRulesForEachStateCaseTypeCombination(string state, string caseType)
    {
        var rules = CourtRuleSeedData.GetAllRules();
        rules.Should().Contain(r => r.State == state && r.CaseType == caseType,
            $"Expected rules for {state} / {caseType}");
    }

    // --- Trigger event type coverage ---

    [Theory]
    [InlineData("FL", TriggerEventType.ServiceDate)]
    [InlineData("FL", TriggerEventType.HearingDate)]
    [InlineData("NY", TriggerEventType.ServiceDate)]
    [InlineData("NY", TriggerEventType.HearingDate)]
    public void GetAllRules_ContainsTriggerTypes(string state, TriggerEventType triggerType)
    {
        var rules = CourtRuleSeedData.GetAllRules();
        rules.Should().Contain(r => r.State == state && r.TriggerEventType == triggerType);
    }

    // --- Florida-specific rule verification ---

    [Fact]
    public void FloridaRules_ResponseToPetition_Is20CalendarDays()
    {
        var rules = CourtRuleSeedData.GetFloridaRules();

        var responseToPetition = rules.Where(r =>
            r.RuleName == "Response to Petition" &&
            r.CaseType == "Divorce").ToList();

        responseToPetition.Should().HaveCount(1);
        var rule = responseToPetition[0];
        rule.DaysFromTrigger.Should().Be(20);
        rule.CountBusinessDays.Should().BeFalse();
        rule.TriggerEventType.Should().Be(TriggerEventType.ServiceDate);
        rule.RuleCitation.Should().Contain("12.140");
    }

    [Fact]
    public void FloridaRules_MandatoryDisclosure_Is45Days()
    {
        var rules = CourtRuleSeedData.GetFloridaRules();

        var mandatoryDisclosure = rules.Where(r =>
            r.RuleName == "Mandatory Financial Disclosure" &&
            r.CaseType == "Divorce").ToList();

        mandatoryDisclosure.Should().HaveCount(1);
        var rule = mandatoryDisclosure[0];
        rule.DaysFromTrigger.Should().Be(45);
        rule.TriggerEventType.Should().Be(TriggerEventType.ServiceDate);
        rule.RuleCitation.Should().Contain("12.285");
    }

    [Fact]
    public void FloridaRules_InterrogatoriesStandard_Is30Days()
    {
        var rules = CourtRuleSeedData.GetFloridaRules();

        var interrogatories = rules.Where(r =>
            r.RuleName == "Response to Interrogatories" &&
            r.CaseType == "Divorce").ToList();

        interrogatories.Should().HaveCount(1);
        interrogatories[0].DaysFromTrigger.Should().Be(30);
        interrogatories[0].RuleCitation.Should().Contain("12.340");
    }

    [Fact]
    public void FloridaRules_InterrogatoriesWithProcess_Is45Days()
    {
        var rules = CourtRuleSeedData.GetFloridaRules();

        var rule = rules.First(r =>
            r.RuleName == "Response to Interrogatories (Served with Process)" &&
            r.CaseType == "Divorce");

        rule.DaysFromTrigger.Should().Be(45);
    }

    [Fact]
    public void FloridaRules_RequestForProduction_Is30Days()
    {
        var rules = CourtRuleSeedData.GetFloridaRules();

        var rule = rules.First(r =>
            r.RuleName == "Response to Request for Production" &&
            r.CaseType == "Divorce");

        rule.DaysFromTrigger.Should().Be(30);
        rule.RuleCitation.Should().Contain("12.350");
    }

    [Fact]
    public void FloridaRules_RequestsForAdmission_Is30Days()
    {
        var rules = CourtRuleSeedData.GetFloridaRules();

        var rule = rules.First(r =>
            r.RuleName == "Response to Requests for Admission" &&
            r.CaseType == "Divorce");

        rule.DaysFromTrigger.Should().Be(30);
        rule.RuleCitation.Should().Contain("1.370");
    }

    [Fact]
    public void FloridaRules_NoticeOfHearing_Is5Days()
    {
        var rules = CourtRuleSeedData.GetFloridaRules();

        var rule = rules.First(r =>
            r.RuleName == "Notice of Hearing on Motion" &&
            r.CaseType == "Divorce");

        rule.DaysFromTrigger.Should().Be(5);
        rule.TriggerEventType.Should().Be(TriggerEventType.HearingDate);
    }

    [Fact]
    public void FloridaRules_MovantTempHearingDocs_Is10Days()
    {
        var rules = CourtRuleSeedData.GetFloridaRules();

        var rule = rules.First(r =>
            r.RuleName == "Movant Financial Docs for Temporary Hearing" &&
            r.CaseType == "Divorce");

        rule.DaysFromTrigger.Should().Be(10);
        rule.TriggerEventType.Should().Be(TriggerEventType.HearingDate);
        rule.RuleCitation.Should().Contain("12.285");
    }

    // --- New York-specific rule verification ---

    [Fact]
    public void NewYorkRules_AnswerPersonalService_Is20Days()
    {
        var rules = CourtRuleSeedData.GetNewYorkRules();

        var rule = rules.First(r =>
            r.RuleName == "Answer to Complaint (Personal Service in NY)" &&
            r.CaseType == "Divorce");

        rule.DaysFromTrigger.Should().Be(20);
        rule.CountBusinessDays.Should().BeFalse();
        rule.RuleCitation.Should().Contain("CPLR § 3012");
    }

    [Fact]
    public void NewYorkRules_AnswerOtherService_Is30Days()
    {
        var rules = CourtRuleSeedData.GetNewYorkRules();

        var rule = rules.First(r =>
            r.RuleName == "Answer to Complaint (Substituted/Other Service)" &&
            r.CaseType == "Divorce");

        rule.DaysFromTrigger.Should().Be(30);
    }

    [Fact]
    public void NewYorkRules_Interrogatories_Is20Days()
    {
        var rules = CourtRuleSeedData.GetNewYorkRules();

        var rule = rules.First(r =>
            r.RuleName == "Response to Interrogatories" &&
            r.CaseType == "Divorce");

        rule.DaysFromTrigger.Should().Be(20);
        rule.RuleCitation.Should().Contain("CPLR § 3133");
    }

    [Fact]
    public void NewYorkRules_DocumentProduction_Is20Days()
    {
        var rules = CourtRuleSeedData.GetNewYorkRules();

        var rule = rules.First(r =>
            r.RuleName == "Response to Document Production Request" &&
            r.CaseType == "Divorce");

        rule.DaysFromTrigger.Should().Be(20);
        rule.RuleCitation.Should().Contain("CPLR § 3120");
    }

    [Fact]
    public void NewYorkRules_ExpertReport_Is60DaysBeforeTrial()
    {
        var rules = CourtRuleSeedData.GetNewYorkRules();

        var rule = rules.First(r =>
            r.RuleName == "Expert Witness Report Exchange" &&
            r.CaseType == "Divorce");

        rule.DaysFromTrigger.Should().Be(60);
        rule.TriggerEventType.Should().Be(TriggerEventType.HearingDate);
        rule.RuleCitation.Should().Contain("22 NYCRR");
    }

    [Fact]
    public void NewYorkRules_ReplyExpertReport_Is30DaysBeforeTrial()
    {
        var rules = CourtRuleSeedData.GetNewYorkRules();

        var rule = rules.First(r =>
            r.RuleName == "Reply Expert Witness Report" &&
            r.CaseType == "Divorce");

        rule.DaysFromTrigger.Should().Be(30);
        rule.TriggerEventType.Should().Be(TriggerEventType.HearingDate);
    }

    [Fact]
    public void NewYorkRules_NoticeOfMotion_Is8Days()
    {
        var rules = CourtRuleSeedData.GetNewYorkRules();

        var rule = rules.First(r =>
            r.RuleName == "Notice of Motion" &&
            r.CaseType == "Divorce");

        rule.DaysFromTrigger.Should().Be(8);
        rule.TriggerEventType.Should().Be(TriggerEventType.HearingDate);
        rule.RuleCitation.Should().Contain("CPLR § 2214");
    }

    [Fact]
    public void NewYorkRules_AnsweringPapers_Is2DaysBeforeReturn()
    {
        var rules = CourtRuleSeedData.GetNewYorkRules();

        var rule = rules.First(r =>
            r.RuleName == "Answering Papers on Motion" &&
            r.CaseType == "Divorce");

        rule.DaysFromTrigger.Should().Be(2);
        rule.TriggerEventType.Should().Be(TriggerEventType.HearingDate);
    }

    // --- Buffer days ---

    [Fact]
    public void AllRules_HaveBufferDays()
    {
        var rules = CourtRuleSeedData.GetAllRules();
        rules.Should().OnlyContain(r => r.BufferDays.HasValue && r.BufferDays > 0,
            "Every seeded rule should include buffer days for internal warnings");
    }

    [Fact]
    public void FloridaRules_MandatoryDisclosure_Has10DayBuffer()
    {
        var rules = CourtRuleSeedData.GetFloridaRules();

        var rule = rules.First(r =>
            r.RuleName == "Mandatory Financial Disclosure" &&
            r.CaseType == "Divorce");

        rule.BufferDays.Should().Be(10);
    }

    [Fact]
    public void NewYorkRules_ExpertReport_Has14DayBuffer()
    {
        var rules = CourtRuleSeedData.GetNewYorkRules();

        var rule = rules.First(r =>
            r.RuleName == "Expert Witness Report Exchange" &&
            r.CaseType == "Divorce");

        rule.BufferDays.Should().Be(14);
    }

    // --- Adoption has limited rules (no discovery/financial) ---

    [Fact]
    public void FloridaRules_Adoption_HasResponseAndHearingRulesOnly()
    {
        var rules = CourtRuleSeedData.GetFloridaRules()
            .Where(r => r.CaseType == "Adoption")
            .ToList();

        rules.Should().Contain(r => r.RuleName == "Response to Petition");
        rules.Should().Contain(r => r.RuleName == "Notice of Hearing on Motion");

        // Adoption is excluded from mandatory financial disclosure
        rules.Should().NotContain(r => r.RuleName == "Mandatory Financial Disclosure");
    }

    // --- SeedAsync ---

    [Fact]
    public async Task SeedAsync_PopulatesDatabase()
    {
        await CourtRuleSeedData.SeedAsync(_context);

        var count = await _context.CourtRules.CountAsync();
        count.Should().BeGreaterThan(0);
        count.Should().Be(CourtRuleSeedData.GetAllRules().Count);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        await CourtRuleSeedData.SeedAsync(_context);
        var firstCount = await _context.CourtRules.CountAsync();

        await CourtRuleSeedData.SeedAsync(_context);
        var secondCount = await _context.CourtRules.CountAsync();

        secondCount.Should().Be(firstCount);
    }

    [Fact]
    public async Task SeedAsync_AllRulesQueryableByState()
    {
        await CourtRuleSeedData.SeedAsync(_context);

        var flRules = await _context.CourtRules.Where(r => r.State == "FL").ToListAsync();
        var nyRules = await _context.CourtRules.Where(r => r.State == "NY").ToListAsync();

        flRules.Should().NotBeEmpty();
        nyRules.Should().NotBeEmpty();
        (flRules.Count + nyRules.Count).Should().Be(await _context.CourtRules.CountAsync());
    }

    // --- Rule count sanity checks ---

    [Fact]
    public void FloridaRules_HasExpectedRuleCount()
    {
        var rules = CourtRuleSeedData.GetFloridaRules();

        // Response rules: 6 case types * 2 (petition + counterpetition) = 12
        // Discovery rules: 5 case types * 6 discovery rules = 30
        // Financial disclosure: 4 case types * 3 (mandatory + 2 temp hearing) = 12
        // Hearing rules: 6 case types * 1 = 6
        // Total: 60
        rules.Should().HaveCount(60);
    }

    [Fact]
    public void NewYorkRules_HasExpectedRuleCount()
    {
        var rules = CourtRuleSeedData.GetNewYorkRules();

        // Response rules: 6 case types * 3 (personal + other + demand) = 18
        // Discovery rules: 5 case types * 4 (interrog + docs + admission + objections) = 20
        // Financial disclosure: 4 case types * 3 (expert + reply + demand) = 12
        // Hearing rules: 6 case types * 2 (motion + answering) = 12
        // Total: 62
        rules.Should().HaveCount(62);
    }
}
