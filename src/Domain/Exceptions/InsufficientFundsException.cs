using Domain.ValueObjects;

namespace Domain.Exceptions;

public class InsufficientFundsException : DomainException
{
    public Money Available { get; }
    public Money Requested { get; }

    public InsufficientFundsException(Money available, Money requested)
        : base($"Insufficient funds. Available: {available.Amount} {available.CurrencyCode}, Requested: {requested.Amount} {requested.CurrencyCode}")
    {
        Available = available;
        Requested = requested;
    }
}
