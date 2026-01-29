using Application.Accounts.Queries.GetAccount;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Application.UnitTests.Accounts.Queries;

public class GetAccountQueryTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly GetAccountQueryHandler _handler;

    public GetAccountQueryTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _handler = new GetAccountQueryHandler(_accountRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_AccountExists_ReturnsAccountDto()
    {
        // Arrange
        var account = Account.Create("John Doe", AccountType.Checking, "USD");
        var query = new GetAccountQuery { AccountId = account.Id };

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(account.Id, result.Id);
        Assert.Equal("John Doe", result.AccountHolderName);
        Assert.Equal(AccountType.Checking, result.Type);
        Assert.Equal(AccountStatus.Active, result.Status);
        Assert.Equal("USD", result.CurrencyCode);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ReturnsNull()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var query = new GetAccountQuery { AccountId = accountId };

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
