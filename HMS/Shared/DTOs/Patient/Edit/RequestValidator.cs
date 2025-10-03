using FluentValidation;

namespace Shared.DTOs.Patient.Edit;

public class RequestValidator : AbstractValidator<Request>
{
    public RequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Length(2, 100).WithMessage("Name must be between 2 and 100 characters.");

        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage("Birth date is required.")
            .LessThan(DateOnly.FromDateTime(DateTime.Today)).WithMessage("Birth date must be before today.")
            .GreaterThan(DateOnly.FromDateTime(DateTime.Today.AddYears(-120))).WithMessage("Birth date is invalid.");

        RuleFor(x => x.Document)
            .NotEmpty().WithMessage("Document is required.")
            .Matches(@"^\d{11}$").WithMessage("Document must be a valid CPF with 11 digits.");

        RuleFor(x => x.Contact)
            .NotEmpty().WithMessage("Contact is required.")
            .Length(2, 100).WithMessage("Contact must be between 2 and 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be in a valid format.")
            .Length(5, 100).WithMessage("Email must be between 5 and 100 characters.");

        RuleFor(x => x.PhoneNumber).MinimumLength(10);
    }
}