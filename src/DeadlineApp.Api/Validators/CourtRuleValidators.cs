using DeadlineApp.Api.DTOs;
using FluentValidation;

namespace DeadlineApp.Api.Validators;

public class CreateCourtRuleRequestValidator : AbstractValidator<CreateCourtRuleRequest>
{
    private static readonly string[] SupportedStates = { "FL", "NY" };

    public CreateCourtRuleRequestValidator()
    {
        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required")
            .Must(s => SupportedStates.Contains(s)).WithMessage("State must be FL or NY");

        RuleFor(x => x.County)
            .MaximumLength(100).WithMessage("County cannot exceed 100 characters");

        RuleFor(x => x.CaseType)
            .NotEmpty().WithMessage("Case type is required")
            .MaximumLength(100).WithMessage("Case type cannot exceed 100 characters");

        RuleFor(x => x.RuleName)
            .NotEmpty().WithMessage("Rule name is required")
            .MaximumLength(200).WithMessage("Rule name cannot exceed 200 characters");

        RuleFor(x => x.RuleDescription)
            .NotEmpty().WithMessage("Rule description is required")
            .MaximumLength(2000).WithMessage("Rule description cannot exceed 2000 characters");

        RuleFor(x => x.RuleCitation)
            .MaximumLength(200).WithMessage("Rule citation cannot exceed 200 characters");

        RuleFor(x => x.TriggerEventType)
            .IsInEnum().WithMessage("Invalid trigger event type");

        RuleFor(x => x.DaysFromTrigger)
            .GreaterThan(0).WithMessage("Days from trigger must be greater than 0");

        RuleFor(x => x.BufferDays)
            .GreaterThan(0).When(x => x.BufferDays.HasValue)
            .WithMessage("Buffer days must be greater than 0 when specified");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty().WithMessage("Effective date is required");

        RuleFor(x => x.ExpirationDate)
            .GreaterThan(x => x.EffectiveDate).When(x => x.ExpirationDate.HasValue)
            .WithMessage("Expiration date must be after effective date");
    }
}

public class UpdateCourtRuleRequestValidator : AbstractValidator<UpdateCourtRuleRequest>
{
    public UpdateCourtRuleRequestValidator()
    {
        RuleFor(x => x.RuleName)
            .NotEmpty().WithMessage("Rule name is required")
            .MaximumLength(200).WithMessage("Rule name cannot exceed 200 characters");

        RuleFor(x => x.RuleDescription)
            .NotEmpty().WithMessage("Rule description is required")
            .MaximumLength(2000).WithMessage("Rule description cannot exceed 2000 characters");

        RuleFor(x => x.RuleCitation)
            .MaximumLength(200).WithMessage("Rule citation cannot exceed 200 characters");

        RuleFor(x => x.DaysFromTrigger)
            .GreaterThan(0).WithMessage("Days from trigger must be greater than 0");

        RuleFor(x => x.BufferDays)
            .GreaterThan(0).When(x => x.BufferDays.HasValue)
            .WithMessage("Buffer days must be greater than 0 when specified");
    }
}
