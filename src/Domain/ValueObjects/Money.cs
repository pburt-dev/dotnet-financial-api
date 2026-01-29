using Domain.Exceptions;

namespace Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with currency.
/// Uses banker's rounding (MidpointRounding.ToEven) for financial calculations.
/// </summary>
public record Money
{
    public decimal Amount { get; }
    public string CurrencyCode { get; }

    public Money(decimal amount, string currencyCode)
    {
        if (amount < 0)
            throw new DomainException("Amount cannot be negative");

        ArgumentNullException.ThrowIfNull(currencyCode);

        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new DomainException("Currency code cannot be empty");

        Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);
        CurrencyCode = currencyCode.ToUpperInvariant();
    }

    public static Money Zero(string currency) => new(0, currency);

    public static Money USD(decimal amount) => new(amount, "USD");

    public static Money EUR(decimal amount) => new(amount, "EUR");

    public static Money GBP(decimal amount) => new(amount, "GBP");

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, CurrencyCode);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);

        if (other.Amount > Amount)
            throw new InsufficientFundsException(this, other);

        return new Money(Amount - other.Amount, CurrencyCode);
    }

    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new DomainException("Cannot multiply money by a negative factor");

        return new Money(Amount * factor, CurrencyCode);
    }

    public bool IsGreaterThan(Money other)
    {
        EnsureSameCurrency(other);
        return Amount > other.Amount;
    }

    public bool IsLessThan(Money other)
    {
        EnsureSameCurrency(other);
        return Amount < other.Amount;
    }

    public bool IsZero => Amount == 0;

    private void EnsureSameCurrency(Money other)
    {
        if (CurrencyCode != other.CurrencyCode)
            throw new CurrencyMismatchException(CurrencyCode, other.CurrencyCode);
    }

    public override string ToString() => $"{Amount:F2} {CurrencyCode}";
}
