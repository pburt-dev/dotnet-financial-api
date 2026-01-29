using FluentValidation;

namespace Application.Accounts.Commands.UnfreezeAccount;

public class UnfreezeAccountCommandValidator : AbstractValidator<UnfreezeAccountCommand>
{
    public UnfreezeAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required");
    }
}
