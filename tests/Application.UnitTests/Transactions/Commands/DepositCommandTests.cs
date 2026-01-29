using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Transactions.Commands.Deposit;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Application.UnitTests.Transactions.Commands;

public class DepositCommandTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly DepositCommandHandler _handler;

    public DepositCommandTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _handler = new DepositCommandHandler(
            _accountRepositoryMock.Object,
            _transactionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesDeposit()
    {
        // Arrange
        var account = Account.Create("John Doe", AccountType.Checking, "USD");
        var command = new DepositCommand
        {
            AccountId = account.Id,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = "deposit-key",
            Description = "Test deposit"
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
        Assert.Equal(100m, result.Amount);
        Assert.Equal("USD", result.CurrencyCode);
        Assert.Equal(TransactionType.Deposit, result.Type);
        Assert.Equal("Test deposit", result.Description);

        _transactionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateIdempotencyKey_ReturnsExistingTransaction()
    {
        // Arrange
        var account = Account.Create("John Doe", AccountType.Checking, "USD");
        var existingTransaction = account.Deposit(Domain.ValueObjects.Money.USD(100m), "deposit-key");

        var command = new DepositCommand
        {
            AccountId = account.Id,
            Amount = 200m, // Different amount
            CurrencyCode = "USD",
            IdempotencyKey = "deposit-key"
        };

        _transactionRepositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(account.Id, command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTransaction);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(existingTransaction.Id, result.Id);
        Assert.Equal(100m, result.Amount); // Returns original amount

        _accountRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var command = new DepositCommand
        {
            AccountId = accountId,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = "deposit-key"
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

public class DepositCommandValidatorTests
{
    private readonly DepositCommandValidator _validator;

    public DepositCommandValidatorTests()
    {
        _validator = new DepositCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ReturnsValid()
    {
        var command = new DepositCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = "deposit-key"
        };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountId_ReturnsInvalid()
    {
        var command = new DepositCommand
        {
            AccountId = Guid.Empty,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = "deposit-key"
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "AccountId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Validate_NonPositiveAmount_ReturnsInvalid(decimal amount)
    {
        var command = new DepositCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = amount,
            CurrencyCode = "USD",
            IdempotencyKey = "deposit-key"
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Validate_AmountExceedsLimit_ReturnsInvalid()
    {
        var command = new DepositCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = 1_000_001m,
            CurrencyCode = "USD",
            IdempotencyKey = "deposit-key"
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Validate_InvalidCurrency_ReturnsInvalid()
    {
        var command = new DepositCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = 100m,
            CurrencyCode = "XYZ",
            IdempotencyKey = "deposit-key"
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CurrencyCode");
    }

    [Fact]
    public void Validate_EmptyIdempotencyKey_ReturnsInvalid()
    {
        var command = new DepositCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = ""
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "IdempotencyKey");
    }

    [Fact]
    public void Validate_IdempotencyKeyTooLong_ReturnsInvalid()
    {
        var command = new DepositCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = new string('A', 65)
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "IdempotencyKey");
    }

    [Fact]
    public void Validate_DescriptionTooLong_ReturnsInvalid()
    {
        var command = new DepositCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = "deposit-key",
            Description = new string('A', 501)
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }
}
