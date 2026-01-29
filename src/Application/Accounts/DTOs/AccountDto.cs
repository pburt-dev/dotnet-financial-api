using Domain.Entities;
using Domain.Enums;

namespace Application.Accounts.DTOs;

public record AccountDto
{
    public Guid Id { get; init; }
    public string AccountNumber { get; init; } = null!;
    public string AccountHolderName { get; init; } = null!;
    public decimal Balance { get; init; }
    public string CurrencyCode { get; init; } = null!;
    public AccountStatus Status { get; init; }
    public AccountType Type { get; init; }
    public DateTime OpenedDate { get; init; }
    public DateTime? ClosedDate { get; init; }
    public string? FreezeReason { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastModifiedAt { get; init; }

    public static AccountDto FromEntity(Account account)
    {
        return new AccountDto
        {
            Id = account.Id,
            AccountNumber = account.AccountNumber,
            AccountHolderName = account.AccountHolderName,
            Balance = account.Balance.Amount,
            CurrencyCode = account.Balance.CurrencyCode,
            Status = account.Status,
            Type = account.Type,
            OpenedDate = account.OpenedDate,
            ClosedDate = account.ClosedDate,
            FreezeReason = account.FreezeReason,
            CreatedAt = account.CreatedAt,
            LastModifiedAt = account.LastModifiedAt
        };
    }
}
