using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Transactions.Commands.Withdraw;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;
using Moq;

namespace Application.UnitTests.Transactions.Commands;

public class WithdrawCommandTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly WithdrawCommandHandler _handler;

    public WithdrawCommandTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _handler = new WithdrawCommandHandler(
            _accountRepositoryMock.Object,
            _transactionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesWithdrawal()
    {
        // Arrange
        var account = Account.Create("John Doe", AccountType.Checking, "USD");
        account.Deposit(Money.USD(200m), "deposit-key");

        var command = new WithdrawCommand
        {
            AccountId = account.Id,
            Amount = 50m,
            CurrencyCode = "USD",
            IdempotencyKey = "withdraw-key",
            Description = "ATM withdrawal"
        };

        _transactionRepositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(account.Id, command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50m, result.Amount);
        Assert.Equal(TransactionType.Withdrawal, result.Type);
        Assert.Equal(150m, result.BalanceAfter);

        _transactionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_InsufficientFunds_ThrowsException()
    {
        // Arrange
        var account = Account.Create("John Doe", AccountType.Checking, "USD");
        account.Deposit(Money.USD(50m), "deposit-key");

        var command = new WithdrawCommand
        {
            AccountId = account.Id,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = "withdraw-key"
        };

        _transactionRepositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(account.Id, command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientFundsException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DuplicateIdempotencyKey_ReturnsExistingTransaction()
    {
        // Arrange
        var account = Account.Create("John Doe", AccountType.Checking, "USD");
        account.Deposit(Money.USD(200m), "deposit-key");
        var existingTransaction = account.Withdraw(Money.USD(50m), "withdraw-key");

        var command = new WithdrawCommand
        {
            AccountId = account.Id,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = "withdraw-key"
        };

        _transactionRepositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(account.Id, command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTransaction);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(existingTransaction.Id, result.Id);
        Assert.Equal(50m, result.Amount);

        _accountRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var command = new WithdrawCommand
        {
            AccountId = accountId,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = "withdraw-key"
        };

        _transactionRepositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(accountId, command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}

public class WithdrawCommandValidatorTests
{
    private readonly WithdrawCommandValidator _validator;

    public WithdrawCommandValidatorTests()
    {
        _validator = new WithdrawCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ReturnsValid()
    {
        var command = new WithdrawCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = "withdraw-key"
        };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Validate_NonPositiveAmount_ReturnsInvalid(decimal amount)
    {
        var command = new WithdrawCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = amount,
            CurrencyCode = "USD",
            IdempotencyKey = "withdraw-key"
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Validate_AmountExceedsLimit_ReturnsInvalid()
    {
        var command = new WithdrawCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = 100_001m,
            CurrencyCode = "USD",
            IdempotencyKey = "withdraw-key"
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }
}
