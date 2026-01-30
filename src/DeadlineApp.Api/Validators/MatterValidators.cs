using DeadlineApp.Api.DTOs;
using FluentValidation;

namespace DeadlineApp.Api.Validators;

public class CreateMatterRequestValidator : AbstractValidator<CreateMatterRequest>
{
    private static readonly string[] SupportedStates = { "FL", "NY" };
    private static readonly string[] SupportedCaseTypes = { "Divorce", "Custody", "Child Support", "Alimony", "Paternity", "Adoption" };

    public CreateMatterRequestValidator()
    {
        RuleFor(x => x.MatterNumber)
            .NotEmpty().WithMessage("Matter number is required")
            .MaximumLength(50).WithMessage("Matter number cannot exceed 50 characters");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(500).WithMessage("Title cannot exceed 500 characters");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required")
            .Must(s => SupportedStates.Contains(s)).WithMessage("State must be FL or NY");

        RuleFor(x => x.County)
            .NotEmpty().WithMessage("County is required")
            .MaximumLength(100).WithMessage("County cannot exceed 100 characters");

        RuleFor(x => x.CaseType)
            .NotEmpty().WithMessage("Case type is required")
            .Must(c => SupportedCaseTypes.Contains(c)).WithMessage($"Case type must be one of: {string.Join(", ", SupportedCaseTypes)}");

        RuleFor(x => x.FilingDate)
            .NotEmpty().WithMessage("Filing date is required")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithMessage("Filing date cannot be in the future");

        RuleFor(x => x.ResponsibleAttorneyId)
            .NotEmpty().WithMessage("Responsible attorney is required");
    }
}

public class UpdateMatterRequestValidator : AbstractValidator<UpdateMatterRequest>
{
    private static readonly string[] SupportedStates = { "FL", "NY" };
    private static readonly string[] SupportedCaseTypes = { "Divorce", "Custody", "Child Support", "Alimony", "Paternity", "Adoption" };

    public UpdateMatterRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(500).WithMessage("Title cannot exceed 500 characters");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required")
            .Must(s => SupportedStates.Contains(s)).WithMessage("State must be FL or NY");

        RuleFor(x => x.County)
            .NotEmpty().WithMessage("County is required")
            .MaximumLength(100).WithMessage("County cannot exceed 100 characters");

        RuleFor(x => x.CaseType)
            .NotEmpty().WithMessage("Case type is required")
            .Must(c => SupportedCaseTypes.Contains(c)).WithMessage($"Case type must be one of: {string.Join(", ", SupportedCaseTypes)}");

        RuleFor(x => x.FilingDate)
            .NotEmpty().WithMessage("Filing date is required");

        RuleFor(x => x.ResponsibleAttorneyId)
            .NotEmpty().WithMessage("Responsible attorney is required");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value");
    }
}
