using DeadlineApp.Api.DTOs;
using DeadlineApp.Api.Validators;
using DeadlineApp.Core.Enums;
using FluentValidation.TestHelper;

namespace DeadlineApp.Api.Tests.Validators;

public class CreateCourtRuleRequestValidatorTests
{
    private readonly CreateCourtRuleRequestValidator _validator;

    public CreateCourtRuleRequestValidatorTests()
    {
        _validator = new CreateCourtRuleRequestValidator();
    }

    private static CreateCourtRuleRequest ValidRequest => new(
        State: "FL",
        County: "Miami-Dade",
        CaseType: "Divorce",
        RuleName: "Response Deadline",
        RuleDescription: "Respondent must file answer within 20 days of service",
        RuleCitation: "Fla. Fam. L. R. P. 12.140",
        TriggerEventType: TriggerEventType.ServiceDate,
        DaysFromTrigger: 20,
        CountBusinessDays: false,
        ExcludeHolidays: true,
        BufferDays: 5,
        EffectiveDate: new DateTime(2020, 1, 1),
        ExpirationDate: null);

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var result = _validator.TestValidate(ValidRequest);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("FL")]
    [InlineData("NY")]
    public void ValidState_PassesValidation(string state)
    {
        var request = ValidRequest with { State = state };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.State);
    }

    [Theory]
    [InlineData("CA")]
    [InlineData("")]
    [InlineData("XX")]
    public void InvalidState_FailsValidation(string state)
    {
        var request = ValidRequest with { State = state };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.State);
    }

    [Fact]
    public void EmptyRuleName_FailsValidation()
    {
        var request = ValidRequest with { RuleName = "" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.RuleName);
    }

    [Fact]
    public void RuleName_ExceedsMaxLength_FailsValidation()
    {
        var request = ValidRequest with { RuleName = new string('A', 201) };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.RuleName);
    }

    [Fact]
    public void EmptyRuleDescription_FailsValidation()
    {
        var request = ValidRequest with { RuleDescription = "" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.RuleDescription);
    }

    [Fact]
    public void ZeroDaysFromTrigger_FailsValidation()
    {
        var request = ValidRequest with { DaysFromTrigger = 0 };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.DaysFromTrigger);
    }

    [Fact]
    public void NegativeDaysFromTrigger_FailsValidation()
    {
        var request = ValidRequest with { DaysFromTrigger = -5 };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.DaysFromTrigger);
    }

    [Fact]
    public void ZeroBufferDays_FailsValidation()
    {
        var request = ValidRequest with { BufferDays = 0 };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.BufferDays);
    }

    [Fact]
    public void NullBufferDays_PassesValidation()
    {
        var request = ValidRequest with { BufferDays = null };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.BufferDays);
    }

    [Fact]
    public void ExpirationBeforeEffective_FailsValidation()
    {
        var request = ValidRequest with
        {
            EffectiveDate = new DateTime(2025, 1, 1),
            ExpirationDate = new DateTime(2024, 1, 1)
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ExpirationDate);
    }

    [Fact]
    public void NullExpirationDate_PassesValidation()
    {
        var request = ValidRequest with { ExpirationDate = null };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.ExpirationDate);
    }

    [Fact]
    public void NullCounty_PassesValidation()
    {
        var request = ValidRequest with { County = null };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.County);
    }

    [Fact]
    public void InvalidTriggerEventType_FailsValidation()
    {
        var request = ValidRequest with { TriggerEventType = (TriggerEventType)99 };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TriggerEventType);
    }
}

public class UpdateCourtRuleRequestValidatorTests
{
    private readonly UpdateCourtRuleRequestValidator _validator;

    public UpdateCourtRuleRequestValidatorTests()
    {
        _validator = new UpdateCourtRuleRequestValidator();
    }

    private static UpdateCourtRuleRequest ValidRequest => new(
        RuleName: "Updated Rule",
        RuleDescription: "Updated description for the rule",
        RuleCitation: "Fla. R. Civ. P. 1.140",
        DaysFromTrigger: 30,
        CountBusinessDays: true,
        ExcludeHolidays: true,
        BufferDays: 7,
        IsActive: true,
        ExpirationDate: null);

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var result = _validator.TestValidate(ValidRequest);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyRuleName_FailsValidation()
    {
        var request = ValidRequest with { RuleName = "" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.RuleName);
    }

    [Fact]
    public void ZeroDaysFromTrigger_FailsValidation()
    {
        var request = ValidRequest with { DaysFromTrigger = 0 };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.DaysFromTrigger);
    }
}
