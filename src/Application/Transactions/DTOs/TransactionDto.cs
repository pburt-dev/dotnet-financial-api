using Domain.Entities;
using Domain.Enums;

namespace Application.Transactions.DTOs;

public record TransactionDto
{
    public Guid Id { get; init; }
    public string TransactionReference { get; init; } = null!;
    public Guid AccountId { get; init; }
    public TransactionType Type { get; init; }
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = null!;
    public decimal BalanceAfter { get; init; }
    public TransactionStatus Status { get; init; }
    public string? Description { get; init; }
    public DateTime ProcessedAt { get; init; }
    public Guid? CounterpartyAccountId { get; init; }

    public static TransactionDto FromEntity(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(transaction.Amount, "transaction.Amount");
        ArgumentNullException.ThrowIfNull(transaction.BalanceAfter, "transaction.BalanceAfter");

        return new TransactionDto
        {
            Id = transaction.Id,
            TransactionReference = transaction.TransactionReference,
            AccountId = transaction.AccountId,
            Type = transaction.Type,
            Amount = transaction.Amount.Amount,
            CurrencyCode = transaction.Amount.CurrencyCode,
            BalanceAfter = transaction.BalanceAfter.Amount,
            Status = transaction.Status,
            Description = transaction.Description,
            ProcessedAt = transaction.ProcessedAt,
            CounterpartyAccountId = transaction.CounterpartyAccountId
        };
    }
}
