using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>
/// Represents a financial transaction on an account.
/// Immutable after creation to ensure audit integrity.
/// </summary>
public class Transaction : BaseAuditableEntity
{
    public Guid Id { get; private set; }
    public string TransactionReference { get; private set; } = null!;
    public Guid AccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; } = null!;
    public Money BalanceAfter { get; private set; } = null!;
    public TransactionStatus Status { get; private set; }
    public string? Description { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public DateTime ProcessedAt { get; private set; }
    public Guid? CounterpartyAccountId { get; private set; }

    private Transaction() { } // For EF Core

    internal static Transaction Create(
        Guid accountId,
        TransactionType type,
        Money amount,
        Money balanceAfter,
        string idempotencyKey,
        string? description = null,
        Guid? counterpartyAccountId = null)
    {
        if (amount.IsZero)
            throw new DomainException("Transaction amount cannot be zero");

        return new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionReference = GenerateTransactionReference(),
            AccountId = accountId,
            Type = type,
            Amount = amount,
            BalanceAfter = balanceAfter,
            Status = TransactionStatus.Completed,
            Description = description,
            IdempotencyKey = idempotencyKey,
            ProcessedAt = DateTime.UtcNow,
            CounterpartyAccountId = counterpartyAccountId
        };
    }

    public void MarkAsFailed(string reason)
    {
        if (Status != TransactionStatus.Pending)
            throw new DomainException("Only pending transactions can be marked as failed");

        Status = TransactionStatus.Failed;
        Description = $"{Description} - Failed: {reason}";
    }

    public void Reverse()
    {
        if (Status != TransactionStatus.Completed)
            throw new DomainException("Only completed transactions can be reversed");

        Status = TransactionStatus.Reversed;
    }

    private static string GenerateTransactionReference()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(10000, 99999);
        return $"TXN-{timestamp}-{random}";
    }
}
