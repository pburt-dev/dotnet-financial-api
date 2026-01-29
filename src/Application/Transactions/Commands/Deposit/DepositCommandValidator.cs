using FluentValidation;

namespace Application.Transactions.Commands.Deposit;

public class DepositCommandValidator : AbstractValidator<DepositCommand>
{
    private static readonly string[] SupportedCurrencies = { "USD", "EUR", "GBP" };

    public DepositCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be positive")
            .LessThanOrEqualTo(1_000_000).WithMessage("Single deposit cannot exceed $1,000,000");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required")
            .Must(BeValidCurrency).WithMessage($"Currency must be one of: {string.Join(", ", SupportedCurrencies)}");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required")
            .MaximumLength(64).WithMessage("Idempotency key cannot exceed 64 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }

    private static bool BeValidCurrency(string currencyCode) =>
        SupportedCurrencies.Contains(currencyCode?.ToUpperInvariant());
}
