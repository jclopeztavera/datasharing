using DeadlineApp.Api.DTOs;
using FluentValidation;

namespace DeadlineApp.Api.Validators;

public class CreateDeadlineRequestValidator : AbstractValidator<CreateDeadlineRequest>
{
    public CreateDeadlineRequestValidator()
    {
        RuleFor(x => x.MatterId)
            .NotEmpty().WithMessage("Matter ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(300).WithMessage("Title cannot exceed 300 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("Due date is required");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid deadline type");
    }
}

public class UpdateDeadlineRequestValidator : AbstractValidator<UpdateDeadlineRequest>
{
    public UpdateDeadlineRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(300).WithMessage("Title cannot exceed 300 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("Due date is required");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value");
    }
}

public class OverrideDeadlineRequestValidator : AbstractValidator<OverrideDeadlineRequest>
{
    public OverrideDeadlineRequestValidator()
    {
        RuleFor(x => x.NewDueDate)
            .NotEmpty().WithMessage("New due date is required");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Override reason is required")
            .MinimumLength(10).WithMessage("Override reason must be at least 10 characters")
            .MaximumLength(1000).WithMessage("Override reason cannot exceed 1000 characters");
    }
}

public class CreateTriggerEventRequestValidator : AbstractValidator<CreateTriggerEventRequest>
{
    public CreateTriggerEventRequestValidator()
    {
        RuleFor(x => x.MatterId)
            .NotEmpty().WithMessage("Matter ID is required");

        RuleFor(x => x.EventType)
            .IsInEnum().WithMessage("Invalid trigger event type");

        RuleFor(x => x.EventDate)
            .NotEmpty().WithMessage("Event date is required");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }
}
