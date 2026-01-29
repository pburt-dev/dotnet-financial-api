using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>
/// Represents a financial account with balance tracking and transaction history.
/// </summary>
public class Account : BaseAuditableEntity
{
    public Guid Id { get; private set; }
    public string AccountNumber { get; private set; } = null!;
    public string AccountHolderName { get; private set; } = null!;
    public Money Balance { get; private set; } = null!;
    public AccountStatus Status { get; private set; }
    public AccountType Type { get; private set; }
    public DateTime OpenedDate { get; private set; }
    public DateTime? ClosedDate { get; private set; }
    public string? FreezeReason { get; private set; }

    private readonly List<Transaction> _transactions = new();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    private Account() { } // For EF Core

    public static Account Create(
        string accountHolderName,
        AccountType type,
        string currencyCode = "USD")
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = GenerateAccountNumber(),
            AccountHolderName = accountHolderName,
            Balance = Money.Zero(currencyCode),
            Status = AccountStatus.Active,
            Type = type,
            OpenedDate = DateTime.UtcNow
        };

        return account;
    }

    public Transaction Deposit(Money amount, string idempotencyKey, string? description = null)
    {
        EnsureAccountIsActive();
        ValidateIdempotencyKey(idempotencyKey);

        var previousBalance = Balance;
        Balance = Balance.Add(amount);

        var transaction = Transaction.Create(
            accountId: Id,
            type: TransactionType.Deposit,
            amount: amount,
            balanceAfter: Balance,
            idempotencyKey: idempotencyKey,
            description: description ?? "Deposit");

        _transactions.Add(transaction);
        return transaction;
    }

    public Transaction Withdraw(Money amount, string idempotencyKey, string? description = null)
    {
        EnsureAccountIsActive();
        ValidateIdempotencyKey(idempotencyKey);

        if (amount.IsGreaterThan(Balance))
            throw new InsufficientFundsException(Balance, amount);

        Balance = Balance.Subtract(amount);

        var transaction = Transaction.Create(
            accountId: Id,
            type: TransactionType.Withdrawal,
            amount: amount,
            balanceAfter: Balance,
            idempotencyKey: idempotencyKey,
            description: description ?? "Withdrawal");

        _transactions.Add(transaction);
        return transaction;
    }

    public Transaction TransferOut(Money amount, Guid destinationAccountId, string idempotencyKey)
    {
        EnsureAccountIsActive();
        ValidateIdempotencyKey(idempotencyKey);

        if (amount.IsGreaterThan(Balance))
            throw new InsufficientFundsException(Balance, amount);

        Balance = Balance.Subtract(amount);

        var transaction = Transaction.Create(
            accountId: Id,
            type: TransactionType.Transfer,
            amount: amount,
            balanceAfter: Balance,
            idempotencyKey: idempotencyKey,
            description: $"Transfer to account",
            counterpartyAccountId: destinationAccountId);

        _transactions.Add(transaction);
        return transaction;
    }

    public Transaction TransferIn(Money amount, Guid sourceAccountId, string idempotencyKey)
    {
        EnsureAccountIsActive();

        Balance = Balance.Add(amount);

        var transaction = Transaction.Create(
            accountId: Id,
            type: TransactionType.Transfer,
            amount: amount,
            balanceAfter: Balance,
            idempotencyKey: $"{idempotencyKey}-in",
            description: $"Transfer from account",
            counterpartyAccountId: sourceAccountId);

        _transactions.Add(transaction);
        return transaction;
    }

    public void Freeze(string reason)
    {
        if (Status == AccountStatus.Closed)
            throw new AccountClosedException(Id);

        if (Status == AccountStatus.Frozen)
            throw new DomainException("Account is already frozen");

        Status = AccountStatus.Frozen;
        FreezeReason = reason;
    }

    public void Unfreeze()
    {
        if (Status == AccountStatus.Closed)
            throw new AccountClosedException(Id);

        if (Status != AccountStatus.Frozen)
            throw new DomainException("Account is not frozen");

        Status = AccountStatus.Active;
        FreezeReason = null;
    }

    public void Close()
    {
        if (Status == AccountStatus.Closed)
            throw new DomainException("Account is already closed");

        if (!Balance.IsZero)
            throw new DomainException("Cannot close account with non-zero balance");

        Status = AccountStatus.Closed;
        ClosedDate = DateTime.UtcNow;
    }

    public void UpdateAccountHolderName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("Account holder name cannot be empty");

        AccountHolderName = newName;
    }

    private void EnsureAccountIsActive()
    {
        if (Status == AccountStatus.Frozen)
            throw new AccountFrozenException(Id, FreezeReason);

        if (Status == AccountStatus.Closed)
            throw new AccountClosedException(Id);
    }

    private void ValidateIdempotencyKey(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new DomainException("Idempotency key is required");

        if (_transactions.Any(t => t.IdempotencyKey == idempotencyKey))
            throw new DuplicateIdempotencyKeyException(idempotencyKey);
    }

    private static string GenerateAccountNumber()
    {
        var random = new Random();
        var part1 = random.Next(1000, 9999);
        var part2 = random.Next(1000, 9999);
        var part3 = random.Next(1000, 9999);
        return $"{part1}-{part2}-{part3}";
    }
}
