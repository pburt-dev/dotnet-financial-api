using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.UnitTests.Entities;

public class AccountTests
{
    [Fact]
    public void Create_WithValidData_CreatesAccount()
    {
        var account = Account.Create("John Doe", AccountType.Checking, "USD");

        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal("John Doe", account.AccountHolderName);
        Assert.Equal(AccountType.Checking, account.Type);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Equal(0m, account.Balance.Amount);
        Assert.Equal("USD", account.Balance.CurrencyCode);
        Assert.NotNull(account.AccountNumber);
        Assert.Matches(@"^\d{4}-\d{4}-\d{4}$", account.AccountNumber);
    }

    [Fact]
    public void Create_WithDifferentCurrency_CreateAccountWithThatCurrency()
    {
        var account = Account.Create("Jane Doe", AccountType.Savings, "EUR");

        Assert.Equal("EUR", account.Balance.CurrencyCode);
    }

    [Theory]
    [InlineData(AccountType.Checking)]
    [InlineData(AccountType.Savings)]
    [InlineData(AccountType.Investment)]
    public void Create_WithDifferentAccountTypes_CreatesCorrectType(AccountType accountType)
    {
        var account = Account.Create("Test User", accountType);

        Assert.Equal(accountType, account.Type);
    }

    #region Deposit Tests

    [Fact]
    public void Deposit_WithValidAmount_IncreasesBalance()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        var depositAmount = Money.USD(100m);

        var transaction = account.Deposit(depositAmount, "key-1");

        Assert.Equal(100m, account.Balance.Amount);
        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal(100m, transaction.Amount.Amount);
        Assert.Equal(100m, transaction.BalanceAfter.Amount);
    }

    [Fact]
    public void Deposit_MultipleDeposits_AccumulatesBalance()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        account.Deposit(Money.USD(100m), "key-1");
        account.Deposit(Money.USD(50m), "key-2");
        account.Deposit(Money.USD(25.50m), "key-3");

        Assert.Equal(175.50m, account.Balance.Amount);
        Assert.Equal(3, account.Transactions.Count);
    }

    [Fact]
    public void Deposit_WithDescription_SetsDescription()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        var transaction = account.Deposit(Money.USD(100m), "key-1", "Payroll deposit");

        Assert.Equal("Payroll deposit", transaction.Description);
    }

    [Fact]
    public void Deposit_OnFrozenAccount_ThrowsAccountFrozenException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Deposit(Money.USD(100m), "key-1");
        account.Freeze("Suspicious activity");

        var exception = Assert.Throws<AccountFrozenException>(
            () => account.Deposit(Money.USD(50m), "key-2"));

        Assert.Equal(account.Id, exception.AccountId);
        Assert.Equal("Suspicious activity", exception.Reason);
    }

    [Fact]
    public void Deposit_OnClosedAccount_ThrowsAccountClosedException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Close();

        var exception = Assert.Throws<AccountClosedException>(
            () => account.Deposit(Money.USD(100m), "key-1"));

        Assert.Equal(account.Id, exception.AccountId);
    }

    [Fact]
    public void Deposit_WithDuplicateIdempotencyKey_ThrowsDuplicateIdempotencyKeyException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Deposit(Money.USD(100m), "key-1");

        var exception = Assert.Throws<DuplicateIdempotencyKeyException>(
            () => account.Deposit(Money.USD(50m), "key-1"));

        Assert.Equal("key-1", exception.IdempotencyKey);
    }

    [Fact]
    public void Deposit_WithEmptyIdempotencyKey_ThrowsDomainException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        var exception = Assert.Throws<DomainException>(
            () => account.Deposit(Money.USD(100m), ""));

        Assert.Equal("Idempotency key is required", exception.Message);
    }

    #endregion

    #region Withdraw Tests

    [Fact]
    public void Withdraw_WithValidAmount_DecreasesBalance()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Deposit(Money.USD(100m), "deposit-key");

        var transaction = account.Withdraw(Money.USD(30m), "withdraw-key");

        Assert.Equal(70m, account.Balance.Amount);
        Assert.Equal(TransactionType.Withdrawal, transaction.Type);
        Assert.Equal(30m, transaction.Amount.Amount);
        Assert.Equal(70m, transaction.BalanceAfter.Amount);
    }

    [Fact]
    public void Withdraw_EntireBalance_LeavesZeroBalance()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Deposit(Money.USD(100m), "deposit-key");

        account.Withdraw(Money.USD(100m), "withdraw-key");

        Assert.Equal(0m, account.Balance.Amount);
        Assert.True(account.Balance.IsZero);
    }

    [Fact]
    public void Withdraw_MoreThanBalance_ThrowsInsufficientFundsException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Deposit(Money.USD(50m), "deposit-key");

        var exception = Assert.Throws<InsufficientFundsException>(
            () => account.Withdraw(Money.USD(100m), "withdraw-key"));

        Assert.Equal(50m, exception.Available.Amount);
        Assert.Equal(100m, exception.Requested.Amount);
    }

    [Fact]
    public void Withdraw_OnFrozenAccount_ThrowsAccountFrozenException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Deposit(Money.USD(100m), "deposit-key");
        account.Freeze("Under review");

        Assert.Throws<AccountFrozenException>(
            () => account.Withdraw(Money.USD(50m), "withdraw-key"));
    }

    [Fact]
    public void Withdraw_OnClosedAccount_ThrowsAccountClosedException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Close();

        Assert.Throws<AccountClosedException>(
            () => account.Withdraw(Money.USD(50m), "withdraw-key"));
    }

    [Fact]
    public void Withdraw_WithDuplicateIdempotencyKey_ThrowsDuplicateIdempotencyKeyException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Deposit(Money.USD(100m), "deposit-key");
        account.Withdraw(Money.USD(30m), "withdraw-key");

        Assert.Throws<DuplicateIdempotencyKeyException>(
            () => account.Withdraw(Money.USD(20m), "withdraw-key"));
    }

    #endregion

    #region Transfer Tests

    [Fact]
    public void TransferOut_WithValidAmount_DecreasesBalance()
    {
        var sourceAccount = Account.Create("John Doe", AccountType.Checking);
        sourceAccount.Deposit(Money.USD(100m), "deposit-key");
        var destinationId = Guid.NewGuid();

        var transaction = sourceAccount.TransferOut(Money.USD(30m), destinationId, "transfer-key");

        Assert.Equal(70m, sourceAccount.Balance.Amount);
        Assert.Equal(TransactionType.Transfer, transaction.Type);
        Assert.Equal(destinationId, transaction.CounterpartyAccountId);
    }

    [Fact]
    public void TransferIn_WithValidAmount_IncreasesBalance()
    {
        var destinationAccount = Account.Create("Jane Doe", AccountType.Savings);
        var sourceId = Guid.NewGuid();

        var transaction = destinationAccount.TransferIn(Money.USD(50m), sourceId, "transfer-key");

        Assert.Equal(50m, destinationAccount.Balance.Amount);
        Assert.Equal(TransactionType.Transfer, transaction.Type);
        Assert.Equal(sourceId, transaction.CounterpartyAccountId);
    }

    [Fact]
    public void TransferOut_MoreThanBalance_ThrowsInsufficientFundsException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Deposit(Money.USD(50m), "deposit-key");

        Assert.Throws<InsufficientFundsException>(
            () => account.TransferOut(Money.USD(100m), Guid.NewGuid(), "transfer-key"));
    }

    #endregion

    #region Freeze/Unfreeze Tests

    [Fact]
    public void Freeze_ActiveAccount_SetsStatusToFrozen()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        account.Freeze("Suspicious activity detected");

        Assert.Equal(AccountStatus.Frozen, account.Status);
        Assert.Equal("Suspicious activity detected", account.FreezeReason);
    }

    [Fact]
    public void Freeze_AlreadyFrozenAccount_ThrowsDomainException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Freeze("First freeze");

        var exception = Assert.Throws<DomainException>(
            () => account.Freeze("Second freeze"));

        Assert.Equal("Account is already frozen", exception.Message);
    }

    [Fact]
    public void Freeze_ClosedAccount_ThrowsAccountClosedException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Close();

        Assert.Throws<AccountClosedException>(
            () => account.Freeze("Trying to freeze closed account"));
    }

    [Fact]
    public void Unfreeze_FrozenAccount_SetsStatusToActive()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Freeze("Temporary hold");

        account.Unfreeze();

        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Null(account.FreezeReason);
    }

    [Fact]
    public void Unfreeze_ActiveAccount_ThrowsDomainException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        var exception = Assert.Throws<DomainException>(() => account.Unfreeze());

        Assert.Equal("Account is not frozen", exception.Message);
    }

    [Fact]
    public void Unfreeze_ClosedAccount_ThrowsAccountClosedException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Close();

        Assert.Throws<AccountClosedException>(() => account.Unfreeze());
    }

    #endregion

    #region Close Tests

    [Fact]
    public void Close_AccountWithZeroBalance_SetsStatusToClosed()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        account.Close();

        Assert.Equal(AccountStatus.Closed, account.Status);
        Assert.NotNull(account.ClosedDate);
    }

    [Fact]
    public void Close_AccountWithBalance_ThrowsDomainException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Deposit(Money.USD(100m), "deposit-key");

        var exception = Assert.Throws<DomainException>(() => account.Close());

        Assert.Equal("Cannot close account with non-zero balance", exception.Message);
    }

    [Fact]
    public void Close_AlreadyClosedAccount_ThrowsDomainException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Close();

        var exception = Assert.Throws<DomainException>(() => account.Close());

        Assert.Equal("Account is already closed", exception.Message);
    }

    [Fact]
    public void Close_FrozenAccountWithZeroBalance_ClosesSuccessfully()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Freeze("Temporary hold");

        // Frozen accounts can be closed if they have zero balance
        // First unfreeze, then close
        account.Unfreeze();
        account.Close();

        Assert.Equal(AccountStatus.Closed, account.Status);
    }

    #endregion

    #region UpdateAccountHolderName Tests

    [Fact]
    public void UpdateAccountHolderName_WithValidName_UpdatesName()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        account.UpdateAccountHolderName("Jane Smith");

        Assert.Equal("Jane Smith", account.AccountHolderName);
    }

    [Fact]
    public void UpdateAccountHolderName_WithEmptyName_ThrowsDomainException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        var exception = Assert.Throws<DomainException>(
            () => account.UpdateAccountHolderName(""));

        Assert.Equal("Account holder name cannot be empty", exception.Message);
    }

    [Fact]
    public void UpdateAccountHolderName_WithWhitespaceName_ThrowsDomainException()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        var exception = Assert.Throws<DomainException>(
            () => account.UpdateAccountHolderName("   "));

        Assert.Equal("Account holder name cannot be empty", exception.Message);
    }

    #endregion

    #region Transaction History Tests

    [Fact]
    public void Transactions_AreReadOnly()
    {
        var account = Account.Create("John Doe", AccountType.Checking);
        account.Deposit(Money.USD(100m), "key-1");

        var transactions = account.Transactions;

        Assert.IsAssignableFrom<IReadOnlyCollection<Transaction>>(transactions);
    }

    [Fact]
    public void Transactions_ContainAllAccountTransactions()
    {
        var account = Account.Create("John Doe", AccountType.Checking);

        account.Deposit(Money.USD(100m), "deposit-1");
        account.Deposit(Money.USD(50m), "deposit-2");
        account.Withdraw(Money.USD(25m), "withdraw-1");

        Assert.Equal(3, account.Transactions.Count);
        Assert.Equal(2, account.Transactions.Count(t => t.Type == TransactionType.Deposit));
        Assert.Equal(1, account.Transactions.Count(t => t.Type == TransactionType.Withdrawal));
    }

    #endregion
}
