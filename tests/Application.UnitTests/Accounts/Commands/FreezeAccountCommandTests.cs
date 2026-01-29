using Application.Accounts.Commands.FreezeAccount;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Application.UnitTests.Accounts.Commands;

public class FreezeAccountCommandTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly FreezeAccountCommandHandler _handler;

    public FreezeAccountCommandTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _handler = new FreezeAccountCommandHandler(_accountRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_FreezesAccount()
    {
        // Arrange
        var account = Account.Create("John Doe", AccountType.Checking);
        var command = new FreezeAccountCommand
        {
            AccountId = account.Id,
            Reason = "Suspicious activity"
        };

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(AccountStatus.Frozen, account.Status);
        Assert.Equal("Suspicious activity", account.FreezeReason);

        _accountRepositoryMock.Verify(
            x => x.UpdateAsync(account, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var command = new FreezeAccountCommand
        {
            AccountId = accountId,
            Reason = "Suspicious activity"
        };

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}

public class FreezeAccountCommandValidatorTests
{
    private readonly FreezeAccountCommandValidator _validator;

    public FreezeAccountCommandValidatorTests()
    {
        _validator = new FreezeAccountCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ReturnsValid()
    {
        var command = new FreezeAccountCommand
        {
            AccountId = Guid.NewGuid(),
            Reason = "Suspicious activity detected"
        };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountId_ReturnsInvalid()
    {
        var command = new FreezeAccountCommand
        {
            AccountId = Guid.Empty,
            Reason = "Suspicious activity"
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "AccountId");
    }

    [Fact]
    public void Validate_EmptyReason_ReturnsInvalid()
    {
        var command = new FreezeAccountCommand
        {
            AccountId = Guid.NewGuid(),
            Reason = ""
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Reason");
    }

    [Fact]
    public void Validate_ReasonTooLong_ReturnsInvalid()
    {
        var command = new FreezeAccountCommand
        {
            AccountId = Guid.NewGuid(),
            Reason = new string('A', 501)
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Reason");
    }
}
