using FluentValidation;

namespace Application.Transactions.Commands.Transfer;

public class TransferCommandValidator : AbstractValidator<TransferCommand>
{
    private static readonly string[] SupportedCurrencies = { "USD", "EUR", "GBP" };

    public TransferCommandValidator()
    {
        RuleFor(x => x.SourceAccountId)
            .NotEmpty().WithMessage("Source account ID is required");

        RuleFor(x => x.DestinationAccountId)
            .NotEmpty().WithMessage("Destination account ID is required")
            .NotEqual(x => x.SourceAccountId).WithMessage("Cannot transfer to the same account");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be positive")
            .LessThanOrEqualTo(500_000).WithMessage("Single transfer cannot exceed $500,000");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required")
            .Must(BeValidCurrency).WithMessage($"Currency must be one of: {string.Join(", ", SupportedCurrencies)}");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required")
            .MaximumLength(64).WithMessage("Idempotency key cannot exceed 64 characters");
    }

    private static bool BeValidCurrency(string currencyCode) =>
        SupportedCurrencies.Contains(currencyCode?.ToUpperInvariant());
}
