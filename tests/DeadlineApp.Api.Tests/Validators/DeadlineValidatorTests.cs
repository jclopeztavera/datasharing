using DeadlineApp.Api.DTOs;
using DeadlineApp.Api.Validators;
using DeadlineApp.Core.Enums;
using FluentValidation.TestHelper;

namespace DeadlineApp.Api.Tests.Validators;

public class CreateDeadlineRequestValidatorTests
{
    private readonly CreateDeadlineRequestValidator _validator;

    public CreateDeadlineRequestValidatorTests()
    {
        _validator = new CreateDeadlineRequestValidator();
    }

    private static CreateDeadlineRequest ValidRequest => new(
        MatterId: Guid.NewGuid(),
        Title: "Response Due",
        Description: "File response to petition",
        DueDate: DateTime.UtcNow.AddDays(20),
        Type: DeadlineType.Court);

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var result = _validator.TestValidate(ValidRequest);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyMatterId_FailsValidation()
    {
        var request = ValidRequest with { MatterId = Guid.Empty };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.MatterId);
    }

    [Fact]
    public void EmptyTitle_FailsValidation()
    {
        var request = ValidRequest with { Title = "" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_ExceedsMaxLength_FailsValidation()
    {
        var request = ValidRequest with { Title = new string('A', 301) };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Description_ExceedsMaxLength_FailsValidation()
    {
        var request = ValidRequest with { Description = new string('A', 2001) };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void InvalidDeadlineType_FailsValidation()
    {
        var request = ValidRequest with { Type = (DeadlineType)99 };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void NullDescription_PassesValidation()
    {
        var request = ValidRequest with { Description = null };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }
}

public class OverrideDeadlineRequestValidatorTests
{
    private readonly OverrideDeadlineRequestValidator _validator;

    public OverrideDeadlineRequestValidatorTests()
    {
        _validator = new OverrideDeadlineRequestValidator();
    }

    private static OverrideDeadlineRequest ValidRequest => new(
        NewDueDate: DateTime.UtcNow.AddDays(30),
        Reason: "Judge granted extension at hearing on January 15");

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var result = _validator.TestValidate(ValidRequest);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyReason_FailsValidation()
    {
        var request = ValidRequest with { Reason = "" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ReasonTooShort_FailsValidation()
    {
        var request = ValidRequest with { Reason = "Short" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ReasonTooLong_FailsValidation()
    {
        var request = ValidRequest with { Reason = new string('A', 1001) };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ReasonAtMinimumLength_PassesValidation()
    {
        var request = ValidRequest with { Reason = new string('A', 10) };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }
}

public class CreateTriggerEventRequestValidatorTests
{
    private readonly CreateTriggerEventRequestValidator _validator;

    public CreateTriggerEventRequestValidatorTests()
    {
        _validator = new CreateTriggerEventRequestValidator();
    }

    private static CreateTriggerEventRequest ValidRequest => new(
        MatterId: Guid.NewGuid(),
        EventType: TriggerEventType.FilingDate,
        EventDate: DateTime.UtcNow,
        Description: "Initial filing");

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var result = _validator.TestValidate(ValidRequest);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyMatterId_FailsValidation()
    {
        var request = ValidRequest with { MatterId = Guid.Empty };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.MatterId);
    }

    [Fact]
    public void InvalidEventType_FailsValidation()
    {
        var request = ValidRequest with { EventType = (TriggerEventType)99 };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.EventType);
    }

    [Fact]
    public void Description_ExceedsMaxLength_FailsValidation()
    {
        var request = ValidRequest with { Description = new string('A', 501) };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void NullDescription_PassesValidation()
    {
        var request = ValidRequest with { Description = null };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Theory]
    [InlineData(TriggerEventType.FilingDate)]
    [InlineData(TriggerEventType.ServiceDate)]
    [InlineData(TriggerEventType.HearingDate)]
    public void AllValidEventTypes_PassValidation(TriggerEventType eventType)
    {
        var request = ValidRequest with { EventType = eventType };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.EventType);
    }
}
