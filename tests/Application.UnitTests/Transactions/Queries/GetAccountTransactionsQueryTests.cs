using Application.Common.Interfaces;
using Application.Transactions.Queries.GetAccountTransactions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Moq;

namespace Application.UnitTests.Transactions.Queries;

public class GetAccountTransactionsQueryTests
{
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly GetAccountTransactionsQueryHandler _handler;

    public GetAccountTransactionsQueryTests()
    {
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _handler = new GetAccountTransactionsQueryHandler(_transactionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsPaginatedTransactions()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = Account.Create("John Doe", AccountType.Checking, "USD");

        var transaction1 = account.Deposit(Money.USD(100m), "key-1");
        var transaction2 = account.Deposit(Money.USD(50m), "key-2");

        var transactions = new List<Transaction> { transaction1, transaction2 };

        var query = new GetAccountTransactionsQuery
        {
            AccountId = accountId,
            PageNumber = 1,
            PageSize = 10
        };

        _transactionRepositoryMock
            .Setup(x => x.GetByAccountIdAsync(accountId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        _transactionRepositoryMock
            .Setup(x => x.GetCountByAccountIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task Handle_MultiplePages_ReturnsCorrectPagination()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = Account.Create("John Doe", AccountType.Checking, "USD");

        var transaction = account.Deposit(Money.USD(100m), "key-1");
        var transactions = new List<Transaction> { transaction };

        var query = new GetAccountTransactionsQuery
        {
            AccountId = accountId,
            PageNumber = 2,
            PageSize = 10
        };

        _transactionRepositoryMock
            .Setup(x => x.GetByAccountIdAsync(accountId, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        _transactionRepositoryMock
            .Setup(x => x.GetCountByAccountIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(25); // 25 total, 3 pages

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task Handle_NoTransactions_ReturnsEmptyList()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        var query = new GetAccountTransactionsQuery
        {
            AccountId = accountId,
            PageNumber = 1,
            PageSize = 10
        };

        _transactionRepositoryMock
            .Setup(x => x.GetByAccountIdAsync(accountId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());

        _transactionRepositoryMock
            .Setup(x => x.GetCountByAccountIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }
}

public class GetAccountTransactionsQueryValidatorTests
{
    private readonly GetAccountTransactionsQueryValidator _validator;

    public GetAccountTransactionsQueryValidatorTests()
    {
        _validator = new GetAccountTransactionsQueryValidator();
    }

    [Fact]
    public void Validate_ValidQuery_ReturnsValid()
    {
        var query = new GetAccountTransactionsQuery
        {
            AccountId = Guid.NewGuid(),
            PageNumber = 1,
            PageSize = 10
        };

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountId_ReturnsInvalid()
    {
        var query = new GetAccountTransactionsQuery
        {
            AccountId = Guid.Empty,
            PageNumber = 1,
            PageSize = 10
        };

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "AccountId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidPageNumber_ReturnsInvalid(int pageNumber)
    {
        var query = new GetAccountTransactionsQuery
        {
            AccountId = Guid.NewGuid(),
            PageNumber = pageNumber,
            PageSize = 10
        };

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PageNumber");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidPageSize_ReturnsInvalid(int pageSize)
    {
        var query = new GetAccountTransactionsQuery
        {
            AccountId = Guid.NewGuid(),
            PageNumber = 1,
            PageSize = pageSize
        };

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void Validate_PageSizeExceedsLimit_ReturnsInvalid()
    {
        var query = new GetAccountTransactionsQuery
        {
            AccountId = Guid.NewGuid(),
            PageNumber = 1,
            PageSize = 101
        };

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PageSize");
    }
}
