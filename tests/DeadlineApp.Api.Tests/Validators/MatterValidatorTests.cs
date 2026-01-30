using DeadlineApp.Api.DTOs;
using DeadlineApp.Api.Validators;
using DeadlineApp.Core.Enums;
using FluentValidation.TestHelper;

namespace DeadlineApp.Api.Tests.Validators;

public class CreateMatterRequestValidatorTests
{
    private readonly CreateMatterRequestValidator _validator;

    public CreateMatterRequestValidatorTests()
    {
        _validator = new CreateMatterRequestValidator();
    }

    private static CreateMatterRequest ValidRequest => new(
        MatterNumber: "FL-2026-001",
        Title: "Smith v. Smith",
        State: "FL",
        County: "Miami-Dade",
        CaseType: "Divorce",
        FilingDate: DateTime.UtcNow.AddDays(-10),
        ResponsibleAttorneyId: Guid.NewGuid());

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var result = _validator.TestValidate(ValidRequest);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyMatterNumber_FailsValidation()
    {
        var request = ValidRequest with { MatterNumber = "" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.MatterNumber);
    }

    [Fact]
    public void MatterNumber_ExceedsMaxLength_FailsValidation()
    {
        var request = ValidRequest with { MatterNumber = new string('A', 51) };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.MatterNumber);
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
        var request = ValidRequest with { Title = new string('A', 501) };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title);
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
    [InlineData("TX")]
    [InlineData("")]
    public void InvalidState_FailsValidation(string state)
    {
        var request = ValidRequest with { State = state };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.State);
    }

    [Fact]
    public void EmptyCounty_FailsValidation()
    {
        var request = ValidRequest with { County = "" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.County);
    }

    [Theory]
    [InlineData("Divorce")]
    [InlineData("Custody")]
    [InlineData("Child Support")]
    [InlineData("Alimony")]
    [InlineData("Paternity")]
    [InlineData("Adoption")]
    public void ValidCaseType_PassesValidation(string caseType)
    {
        var request = ValidRequest with { CaseType = caseType };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.CaseType);
    }

    [Theory]
    [InlineData("Criminal")]
    [InlineData("")]
    [InlineData("Contract")]
    public void InvalidCaseType_FailsValidation(string caseType)
    {
        var request = ValidRequest with { CaseType = caseType };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CaseType);
    }

    [Fact]
    public void FutureFilingDate_FailsValidation()
    {
        var request = ValidRequest with { FilingDate = DateTime.UtcNow.AddDays(30) };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FilingDate);
    }

    [Fact]
    public void EmptyResponsibleAttorneyId_FailsValidation()
    {
        var request = ValidRequest with { ResponsibleAttorneyId = Guid.Empty };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ResponsibleAttorneyId);
    }
}

public class UpdateMatterRequestValidatorTests
{
    private readonly UpdateMatterRequestValidator _validator;

    public UpdateMatterRequestValidatorTests()
    {
        _validator = new UpdateMatterRequestValidator();
    }

    private static UpdateMatterRequest ValidRequest => new(
        Title: "Smith v. Smith - Updated",
        State: "FL",
        County: "Broward",
        CaseType: "Divorce",
        FilingDate: DateTime.UtcNow.AddDays(-10),
        ResponsibleAttorneyId: Guid.NewGuid(),
        Status: MatterStatus.Active);

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var result = _validator.TestValidate(ValidRequest);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void InvalidStatus_FailsValidation()
    {
        var request = ValidRequest with { Status = (MatterStatus)99 };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void InvalidState_FailsValidation()
    {
        var request = ValidRequest with { State = "CA" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.State);
    }
}
