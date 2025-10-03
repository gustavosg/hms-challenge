using FluentValidation;

namespace Shared.DTOs.Users.Edit;

public class RequestValidator : AbstractValidator<Request>
{
    public RequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID is required.");

        RuleFor(x => x.PhoneNumber).MinimumLength(10);
    }
}