using FluentValidation;

namespace Application.Accounts.Commands.FreezeAccount;

public class FreezeAccountCommandValidator : AbstractValidator<FreezeAccountCommand>
{
    public FreezeAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Freeze reason is required")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}
