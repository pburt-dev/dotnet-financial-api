using Application.Accounts.Commands.CreateAccount;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Application.UnitTests.Accounts.Commands;

public class CreateAccountCommandTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly CreateAccountCommandHandler _handler;

    public CreateAccountCommandTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _handler = new CreateAccountCommandHandler(_accountRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesAccount()
    {
        // Arrange
        var command = new CreateAccountCommand
        {
            AccountHolderName = "John Doe",
            Type = AccountType.Checking,
            CurrencyCode = "USD"
        };

        _accountRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account account, CancellationToken _) => account);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.AccountHolderName);
        Assert.Equal(AccountType.Checking, result.Type);
        Assert.Equal("USD", result.CurrencyCode);
        Assert.Equal(0m, result.Balance);
        Assert.Equal(AccountStatus.Active, result.Status);

        _accountRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEURCurrency_CreatesAccountWithEUR()
    {
        // Arrange
        var command = new CreateAccountCommand
        {
            AccountHolderName = "Jane Doe",
            Type = AccountType.Savings,
            CurrencyCode = "EUR"
        };

        _accountRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account account, CancellationToken _) => account);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("EUR", result.CurrencyCode);
    }
}

public class CreateAccountCommandValidatorTests
{
    private readonly CreateAccountCommandValidator _validator;

    public CreateAccountCommandValidatorTests()
    {
        _validator = new CreateAccountCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ReturnsValid()
    {
        var command = new CreateAccountCommand
        {
            AccountHolderName = "John Doe",
            Type = AccountType.Checking,
            CurrencyCode = "USD"
        };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountHolderName_ReturnsInvalid()
    {
        var command = new CreateAccountCommand
        {
            AccountHolderName = "",
            Type = AccountType.Checking,
            CurrencyCode = "USD"
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "AccountHolderName");
    }

    [Fact]
    public void Validate_AccountHolderNameTooLong_ReturnsInvalid()
    {
        var command = new CreateAccountCommand
        {
            AccountHolderName = new string('A', 201),
            Type = AccountType.Checking,
            CurrencyCode = "USD"
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "AccountHolderName");
    }

    [Fact]
    public void Validate_InvalidCurrency_ReturnsInvalid()
    {
        var command = new CreateAccountCommand
        {
            AccountHolderName = "John Doe",
            Type = AccountType.Checking,
            CurrencyCode = "XYZ"
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CurrencyCode");
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("usd")]
    [InlineData("eur")]
    public void Validate_SupportedCurrencies_ReturnsValid(string currency)
    {
        var command = new CreateAccountCommand
        {
            AccountHolderName = "John Doe",
            Type = AccountType.Checking,
            CurrencyCode = currency
        };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
