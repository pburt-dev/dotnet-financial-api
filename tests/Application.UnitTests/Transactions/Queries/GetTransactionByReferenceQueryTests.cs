using Application.Common.Interfaces;
using Application.Transactions.Queries.GetTransactionByReference;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Moq;

namespace Application.UnitTests.Transactions.Queries;

public class GetTransactionByReferenceQueryTests
{
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly GetTransactionByReferenceQueryHandler _handler;

    public GetTransactionByReferenceQueryTests()
    {
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _handler = new GetTransactionByReferenceQueryHandler(_transactionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_TransactionExists_ReturnsTransactionDto()
    {
        // Arrange
        var account = Account.Create("John Doe", AccountType.Checking, "USD");
        var transaction = account.Deposit(Money.USD(100m), "deposit-key");

        var query = new GetTransactionByReferenceQuery
        {
            TransactionReference = transaction.TransactionReference
        };

        _transactionRepositoryMock
            .Setup(x => x.GetByReferenceAsync(transaction.TransactionReference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(transaction.Id, result.Id);
        Assert.Equal(transaction.TransactionReference, result.TransactionReference);
    }

    [Fact]
    public async Task Handle_TransactionNotFound_ReturnsNull()
    {
        // Arrange
        var query = new GetTransactionByReferenceQuery
        {
            TransactionReference = "TXN-20240115-12345"
        };

        _transactionRepositoryMock
            .Setup(x => x.GetByReferenceAsync(query.TransactionReference, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
