using Application.Accounts.Commands.CloseAccount;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Application.UnitTests.Accounts.Commands;

public class CloseAccountCommandTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly CloseAccountCommandHandler _handler;

    public CloseAccountCommandTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _handler = new CloseAccountCommandHandler(_accountRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ClosesAccount()
    {
        // Arrange
        var account = Account.Create("John Doe", AccountType.Checking);
        var command = new CloseAccountCommand { AccountId = account.Id };

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(AccountStatus.Closed, account.Status);
        Assert.NotNull(account.ClosedDate);

        _accountRepositoryMock.Verify(
            x => x.UpdateAsync(account, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var command = new CloseAccountCommand { AccountId = accountId };

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}

public class CloseAccountCommandValidatorTests
{
    private readonly CloseAccountCommandValidator _validator;

    public CloseAccountCommandValidatorTests()
    {
        _validator = new CloseAccountCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ReturnsValid()
    {
        var command = new CloseAccountCommand { AccountId = Guid.NewGuid() };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountId_ReturnsInvalid()
    {
        var command = new CloseAccountCommand { AccountId = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "AccountId");
    }
}
