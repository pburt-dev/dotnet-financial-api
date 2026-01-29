using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Transactions.Commands.Transfer;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;
using Moq;

namespace Application.UnitTests.Transactions.Commands;

public class TransferCommandTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly TransferCommandHandler _handler;

    public TransferCommandTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _handler = new TransferCommandHandler(
            _accountRepositoryMock.Object,
            _transactionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_TransfersFunds()
    {
        // Arrange
        var sourceAccount = Account.Create("John Doe", AccountType.Checking, "USD");
        sourceAccount.Deposit(Money.USD(500m), "deposit-key");

        var destinationAccount = Account.Create("Jane Doe", AccountType.Savings, "USD");

        var command = new TransferCommand
        {
            SourceAccountId = sourceAccount.Id,
            DestinationAccountId = destinationAccount.Id,
            Amount = 200m,
            CurrencyCode = "USD",
            IdempotencyKey = "transfer-key"
        };

        _transactionRepositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(sourceAccount.Id, command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        _accountRepositoryMock
            .Setup(x => x.GetByIdWithTransactionsAsync(sourceAccount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceAccount);

        _accountRepositoryMock
            .Setup(x => x.GetByIdWithTransactionsAsync(destinationAccount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destinationAccount);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.SourceTransaction);
        Assert.NotNull(result.DestinationTransaction);

        Assert.Equal(200m, result.SourceTransaction.Amount);
        Assert.Equal(300m, result.SourceTransaction.BalanceAfter);
        Assert.Equal(TransactionType.Transfer, result.SourceTransaction.Type);

        Assert.Equal(200m, result.DestinationTransaction.Amount);
        Assert.Equal(200m, result.DestinationTransaction.BalanceAfter);
        Assert.Equal(TransactionType.Transfer, result.DestinationTransaction.Type);

        _transactionRepositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_InsufficientFunds_ThrowsException()
    {
        // Arrange
        var sourceAccount = Account.Create("John Doe", AccountType.Checking, "USD");
        sourceAccount.Deposit(Money.USD(100m), "deposit-key");

        var destinationAccount = Account.Create("Jane Doe", AccountType.Savings, "USD");

        var command = new TransferCommand
        {
            SourceAccountId = sourceAccount.Id,
            DestinationAccountId = destinationAccount.Id,
            Amount = 200m,
            CurrencyCode = "USD",
            IdempotencyKey = "transfer-key"
        };

        _transactionRepositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(sourceAccount.Id, command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        _accountRepositoryMock
            .Setup(x => x.GetByIdWithTransactionsAsync(sourceAccount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceAccount);

        _accountRepositoryMock
            .Setup(x => x.GetByIdWithTransactionsAsync(destinationAccount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destinationAccount);

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientFundsException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SourceAccountNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var sourceAccountId = Guid.NewGuid();
        var destinationAccountId = Guid.NewGuid();

        var command = new TransferCommand
        {
            SourceAccountId = sourceAccountId,
            DestinationAccountId = destinationAccountId,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = "transfer-key"
        };

        _transactionRepositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(sourceAccountId, command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        _accountRepositoryMock
            .Setup(x => x.GetByIdWithTransactionsAsync(sourceAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DestinationAccountNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var sourceAccount = Account.Create("John Doe", AccountType.Checking, "USD");
        sourceAccount.Deposit(Money.USD(500m), "deposit-key");

        var destinationAccountId = Guid.NewGuid();

        var command = new TransferCommand
        {
            SourceAccountId = sourceAccount.Id,
            DestinationAccountId = destinationAccountId,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = "transfer-key"
        };

        _transactionRepositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(sourceAccount.Id, command.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        _accountRepositoryMock
            .Setup(x => x.GetByIdWithTransactionsAsync(sourceAccount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceAccount);

        _accountRepositoryMock
            .Setup(x => x.GetByIdWithTransactionsAsync(destinationAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}

public class TransferCommandValidatorTests
{
    private readonly TransferCommandValidator _validator;

    public TransferCommandValidatorTests()
    {
        _validator = new TransferCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ReturnsValid()
    {
        var command = new TransferCommand
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = "transfer-key"
        };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptySourceAccountId_ReturnsInvalid()
    {
        var command = new TransferCommand
        {
            SourceAccountId = Guid.Empty,
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = "transfer-key"
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SourceAccountId");
    }

    [Fact]
    public void Validate_EmptyDestinationAccountId_ReturnsInvalid()
    {
        var command = new TransferCommand
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.Empty,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = "transfer-key"
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DestinationAccountId");
    }

    [Fact]
    public void Validate_SameSourceAndDestination_ReturnsInvalid()
    {
        var accountId = Guid.NewGuid();
        var command = new TransferCommand
        {
            SourceAccountId = accountId,
            DestinationAccountId = accountId,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = "transfer-key"
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DestinationAccountId");
    }

    [Fact]
    public void Validate_AmountExceedsLimit_ReturnsInvalid()
    {
        var command = new TransferCommand
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 500_001m,
            CurrencyCode = "USD",
            IdempotencyKey = "transfer-key"
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }
}
