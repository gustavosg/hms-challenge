using FluentValidation;

namespace Shared.DTOs.Auth;

public class LoginValidator : AbstractValidator<Login>
{
    public LoginValidator()
    {
        RuleFor(_ => _.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be in a valid format.")
            .Length(5, 100).WithMessage("Email must be between 5 and 100 characters.");

        RuleFor(_ => _.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
    }
}
