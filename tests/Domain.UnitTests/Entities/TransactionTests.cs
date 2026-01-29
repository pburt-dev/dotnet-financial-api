using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.UnitTests.Entities;

public class TransactionTests
{
    [Fact]
    public void Transaction_CreatedViaDeposit_HasCorrectProperties()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        var transaction = account.Deposit(Money.USD(100m), "deposit-key", "Payroll");

        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal(account.Id, transaction.AccountId);
        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal(100m, transaction.Amount.Amount);
        Assert.Equal("USD", transaction.Amount.CurrencyCode);
        Assert.Equal(100m, transaction.BalanceAfter.Amount);
        Assert.Equal(TransactionStatus.Completed, transaction.Status);
        Assert.Equal("Payroll", transaction.Description);
        Assert.Equal("deposit-key", transaction.IdempotencyKey);
        Assert.Null(transaction.CounterpartyAccountId);
    }

    [Fact]
    public void Transaction_CreatedViaWithdraw_HasCorrectProperties()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Deposit(Money.USD(100m), "deposit-key");

        var transaction = account.Withdraw(Money.USD(30m), "withdraw-key", "ATM withdrawal");

        Assert.Equal(TransactionType.Withdrawal, transaction.Type);
        Assert.Equal(30m, transaction.Amount.Amount);
        Assert.Equal(70m, transaction.BalanceAfter.Amount);
        Assert.Equal("ATM withdrawal", transaction.Description);
    }

    [Fact]
    public void Transaction_CreatedViaTransferOut_HasCounterpartyAccountId()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Deposit(Money.USD(100m), "deposit-key");
        var destinationAccountId = Guid.NewGuid();

        var transaction = account.TransferOut(Money.USD(50m), destinationAccountId, "transfer-key");

        Assert.Equal(TransactionType.Transfer, transaction.Type);
        Assert.Equal(destinationAccountId, transaction.CounterpartyAccountId);
    }

    [Fact]
    public void Transaction_CreatedViaTransferIn_HasCounterpartyAccountId()
    {
        var account = Account.Create("Jane Doe", AccountType.Savings);
        var sourceAccountId = Guid.NewGuid();

        var transaction = account.TransferIn(Money.USD(50m), sourceAccountId, "transfer-key");

        Assert.Equal(TransactionType.Transfer, transaction.Type);
        Assert.Equal(sourceAccountId, transaction.CounterpartyAccountId);
    }

    [Fact]
    public void TransactionReference_HasCorrectFormat()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        var transaction = account.Deposit(Money.USD(100m), "deposit-key");

        Assert.StartsWith("TXN-", transaction.TransactionReference);
        Assert.Matches(@"^TXN-\d{14}-\d{5}$", transaction.TransactionReference);
    }

    [Fact]
    public void ProcessedAt_IsSetOnCreation()
    {
        var beforeCreation = DateTime.UtcNow;
        var account = Account.Create("John Doe", AccountType.Checking);

        var transaction = account.Deposit(Money.USD(100m), "deposit-key");

        Assert.True(transaction.ProcessedAt >= beforeCreation);
        Assert.True(transaction.ProcessedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void MultipleTransactions_HaveUniqueReferences()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        var transaction1 = account.Deposit(Money.USD(100m), "key-1");
        var transaction2 = account.Deposit(Money.USD(50m), "key-2");
        var transaction3 = account.Deposit(Money.USD(25m), "key-3");

        var references = new[] { transaction1.TransactionReference, transaction2.TransactionReference, transaction3.TransactionReference };

        Assert.Equal(3, references.Distinct().Count());
    }

    [Fact]
    public void MultipleTransactions_HaveUniqueIds()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        var transaction1 = account.Deposit(Money.USD(100m), "key-1");
        var transaction2 = account.Deposit(Money.USD(50m), "key-2");
        var transaction3 = account.Deposit(Money.USD(25m), "key-3");

        var ids = new[] { transaction1.Id, transaction2.Id, transaction3.Id };

        Assert.Equal(3, ids.Distinct().Count());
    }

    [Fact]
    public void Transaction_BalanceAfter_ReflectsAccountBalanceAtTimeOfTransaction()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        var deposit1 = account.Deposit(Money.USD(100m), "key-1");
        var deposit2 = account.Deposit(Money.USD(50m), "key-2");
        var withdraw1 = account.Withdraw(Money.USD(30m), "key-3");

        Assert.Equal(100m, deposit1.BalanceAfter.Amount);
        Assert.Equal(150m, deposit2.BalanceAfter.Amount);
        Assert.Equal(120m, withdraw1.BalanceAfter.Amount);
    }

    [Fact]
    public void Transaction_DefaultDescription_IsSetForDeposit()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        var transaction = account.Deposit(Money.USD(100m), "deposit-key");

        Assert.Equal("Deposit", transaction.Description);
    }

    [Fact]
    public void Transaction_DefaultDescription_IsSetForWithdrawal()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Deposit(Money.USD(100m), "deposit-key");

        var transaction = account.Withdraw(Money.USD(50m), "withdraw-key");

        Assert.Equal("Withdrawal", transaction.Description);
    }

    [Fact]
    public void Transaction_CustomDescription_OverridesDefault()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        var transaction = account.Deposit(Money.USD(100m), "deposit-key", "Direct deposit from employer");

        Assert.Equal("Direct deposit from employer", transaction.Description);
    }

    [Fact]
    public void TransferIn_IdempotencyKey_HasSuffix()
    {
        var account = Account.Create("Jane Doe", AccountType.Savings);
        var sourceAccountId = Guid.NewGuid();

        var transaction = account.TransferIn(Money.USD(50m), sourceAccountId, "transfer-key");

        Assert.Equal("transfer-key-in", transaction.IdempotencyKey);
    }
}
