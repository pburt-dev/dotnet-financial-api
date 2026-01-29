using FluentValidation;

namespace Application.Accounts.Commands.CreateAccount;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    private static readonly string[] SupportedCurrencies = { "USD", "EUR", "GBP" };

    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.AccountHolderName)
            .NotEmpty().WithMessage("Account holder name is required")
            .MaximumLength(200).WithMessage("Account holder name cannot exceed 200 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid account type");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required")
            .Must(BeValidCurrency).WithMessage($"Currency must be one of: {string.Join(", ", SupportedCurrencies)}");
    }

    private static bool BeValidCurrency(string currencyCode) =>
        SupportedCurrencies.Contains(currencyCode?.ToUpperInvariant());
}
