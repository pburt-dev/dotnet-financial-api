using Application.Common.Interfaces;
using Application.Transactions.Queries.GetTransaction;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Moq;

namespace Application.UnitTests.Transactions.Queries;

public class GetTransactionQueryTests
{
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly GetTransactionQueryHandler _handler;

    public GetTransactionQueryTests()
    {
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _handler = new GetTransactionQueryHandler(_transactionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_TransactionExists_ReturnsTransactionDto()
    {
        // Arrange
        var account = Account.Create("John Doe", AccountType.Checking, "USD");
        var transaction = account.Deposit(Money.USD(100m), "deposit-key", "Test deposit");

        var query = new GetTransactionQuery { TransactionId = transaction.Id };

        _transactionRepositoryMock
            .Setup(x => x.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(transaction.Id, result.Id);
        Assert.Equal(100m, result.Amount);
        Assert.Equal("USD", result.CurrencyCode);
        Assert.Equal(TransactionType.Deposit, result.Type);
        Assert.Equal("Test deposit", result.Description);
    }

    [Fact]
    public async Task Handle_TransactionNotFound_ReturnsNull()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var query = new GetTransactionQuery { TransactionId = transactionId };

        _transactionRepositoryMock
            .Setup(x => x.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
