using Domain.Enums;

namespace Domain.Events;

public sealed class TransactionCompletedEvent : DomainEvent
{
    public Guid TransactionId { get; }
    public Guid AccountId { get; }
    public TransactionType TransactionType { get; }
    public decimal Amount { get; }
    public string CurrencyCode { get; }

    public TransactionCompletedEvent(
        Guid transactionId,
        Guid accountId,
        TransactionType transactionType,
        decimal amount,
        string currencyCode)
    {
        TransactionId = transactionId;
        AccountId = accountId;
        TransactionType = transactionType;
        Amount = amount;
        CurrencyCode = currencyCode;
    }
}
