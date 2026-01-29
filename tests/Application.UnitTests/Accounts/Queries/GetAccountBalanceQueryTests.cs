using Application.Accounts.Queries.GetAccountBalance;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Moq;

namespace Application.UnitTests.Accounts.Queries;

public class GetAccountBalanceQueryTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IDateTimeService> _dateTimeServiceMock;
    private readonly GetAccountBalanceQueryHandler _handler;

    public GetAccountBalanceQueryTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _dateTimeServiceMock = new Mock<IDateTimeService>();
        _handler = new GetAccountBalanceQueryHandler(
            _accountRepositoryMock.Object,
            _dateTimeServiceMock.Object);
    }

    [Fact]
    public async Task Handle_AccountExists_ReturnsBalanceDto()
    {
        // Arrange
        var account = Account.Create("John Doe", AccountType.Checking, "USD");
        account.Deposit(Money.USD(150.50m), "deposit-key");

        var query = new GetAccountBalanceQuery { AccountId = account.Id };
        var now = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _dateTimeServiceMock
            .Setup(x => x.UtcNow)
            .Returns(now);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(account.Id, result.AccountId);
        Assert.Equal(account.AccountNumber, result.AccountNumber);
        Assert.Equal(150.50m, result.Balance);
        Assert.Equal("USD", result.CurrencyCode);
        Assert.Equal(now, result.AsOf);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ReturnsNull()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var query = new GetAccountBalanceQuery { AccountId = accountId };

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ZeroBalance_ReturnsZero()
    {
        // Arrange
        var account = Account.Create("John Doe", AccountType.Checking, "USD");
        var query = new GetAccountBalanceQuery { AccountId = account.Id };

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _dateTimeServiceMock
            .Setup(x => x.UtcNow)
            .Returns(DateTime.UtcNow);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0m, result.Balance);
    }
}
