using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Accounts.DTOs;
using Domain.Enums;

namespace API.IntegrationTests.Controllers;

public class AccountsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public AccountsControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    #region Create Account Tests

    [Fact]
    public async Task CreateAccount_ValidRequest_ReturnsCreatedAccount()
    {
        // Arrange
        var request = new
        {
            AccountHolderName = "John Doe",
            Type = AccountType.Checking,
            CurrencyCode = "USD"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/accounts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var account = await response.Content.ReadFromJsonAsync<AccountDto>(_jsonOptions);
        Assert.NotNull(account);
        Assert.Equal("John Doe", account.AccountHolderName);
        Assert.Equal(AccountType.Checking, account.Type);
        Assert.Equal("USD", account.CurrencyCode);
        Assert.Equal(0m, account.Balance);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.NotNull(account.AccountNumber);
    }

    [Fact]
    public async Task CreateAccount_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            AccountHolderName = "",
            Type = AccountType.Checking,
            CurrencyCode = "USD"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/accounts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_InvalidCurrency_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            AccountHolderName = "John Doe",
            Type = AccountType.Checking,
            CurrencyCode = "XYZ"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/accounts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(AccountType.Checking)]
    [InlineData(AccountType.Savings)]
    [InlineData(AccountType.Investment)]
    public async Task CreateAccount_DifferentTypes_ReturnsCorrectType(AccountType accountType)
    {
        // Arrange
        var request = new
        {
            AccountHolderName = "Jane Doe",
            Type = accountType,
            CurrencyCode = "USD"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/accounts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var account = await response.Content.ReadFromJsonAsync<AccountDto>(_jsonOptions);
        Assert.NotNull(account);
        Assert.Equal(accountType, account.Type);
    }

    #endregion

    #region Get Account Tests

    [Fact]
    public async Task GetAccount_ExistingAccount_ReturnsAccount()
    {
        // Arrange
        var createRequest = new
        {
            AccountHolderName = "John Doe",
            Type = AccountType.Checking,
            CurrencyCode = "USD"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/accounts", createRequest);
        var createdAccount = await createResponse.Content.ReadFromJsonAsync<AccountDto>(_jsonOptions);

        // Act
        var response = await _client.GetAsync($"/api/accounts/{createdAccount!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var account = await response.Content.ReadFromJsonAsync<AccountDto>(_jsonOptions);
        Assert.NotNull(account);
        Assert.Equal(createdAccount.Id, account.Id);
        Assert.Equal("John Doe", account.AccountHolderName);
    }

    [Fact]
    public async Task GetAccount_NonExistingAccount_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/accounts/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Get Balance Tests

    [Fact]
    public async Task GetBalance_ExistingAccount_ReturnsBalance()
    {
        // Arrange
        var createRequest = new
        {
            AccountHolderName = "John Doe",
            Type = AccountType.Checking,
            CurrencyCode = "USD"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/accounts", createRequest);
        var createdAccount = await createResponse.Content.ReadFromJsonAsync<AccountDto>(_jsonOptions);

        // Act
        var response = await _client.GetAsync($"/api/accounts/{createdAccount!.Id}/balance");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var balance = await response.Content.ReadFromJsonAsync<AccountBalanceDto>(_jsonOptions);
        Assert.NotNull(balance);
        Assert.Equal(createdAccount.Id, balance.AccountId);
        Assert.Equal(0m, balance.Balance);
        Assert.Equal("USD", balance.CurrencyCode);
    }

    [Fact]
    public async Task GetBalance_NonExistingAccount_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/accounts/{Guid.NewGuid()}/balance");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Freeze Account Tests

    [Fact]
    public async Task FreezeAccount_ExistingAccount_ReturnsNoContent()
    {
        // Arrange
        var createRequest = new
        {
            AccountHolderName = "John Doe",
            Type = AccountType.Checking,
            CurrencyCode = "USD"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/accounts", createRequest);
        var createdAccount = await createResponse.Content.ReadFromJsonAsync<AccountDto>(_jsonOptions);

        var freezeRequest = new { Reason = "Suspicious activity" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/accounts/{createdAccount!.Id}/freeze", freezeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify account is frozen
        var getResponse = await _client.GetAsync($"/api/accounts/{createdAccount.Id}");
        var account = await getResponse.Content.ReadFromJsonAsync<AccountDto>(_jsonOptions);
        Assert.Equal(AccountStatus.Frozen, account!.Status);
        Assert.Equal("Suspicious activity", account.FreezeReason);
    }

    [Fact]
    public async Task FreezeAccount_NonExistingAccount_ReturnsNotFound()
    {
        // Arrange
        var freezeRequest = new { Reason = "Suspicious activity" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/accounts/{Guid.NewGuid()}/freeze", freezeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Unfreeze Account Tests

    [Fact]
    public async Task UnfreezeAccount_FrozenAccount_ReturnsNoContent()
    {
        // Arrange
        var createRequest = new
        {
            AccountHolderName = "John Doe",
            Type = AccountType.Checking,
            CurrencyCode = "USD"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/accounts", createRequest);
        var createdAccount = await createResponse.Content.ReadFromJsonAsync<AccountDto>(_jsonOptions);

        // Freeze the account
        var freezeRequest = new { Reason = "Temporary hold" };
        await _client.PostAsJsonAsync($"/api/accounts/{createdAccount!.Id}/freeze", freezeRequest);

        // Act
        var response = await _client.PostAsync($"/api/accounts/{createdAccount.Id}/unfreeze", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify account is active
        var getResponse = await _client.GetAsync($"/api/accounts/{createdAccount.Id}");
        var account = await getResponse.Content.ReadFromJsonAsync<AccountDto>(_jsonOptions);
        Assert.Equal(AccountStatus.Active, account!.Status);
    }

    #endregion

    #region Close Account Tests

    [Fact]
    public async Task CloseAccount_ZeroBalanceAccount_ReturnsNoContent()
    {
        // Arrange
        var createRequest = new
        {
            AccountHolderName = "John Doe",
            Type = AccountType.Checking,
            CurrencyCode = "USD"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/accounts", createRequest);
        var createdAccount = await createResponse.Content.ReadFromJsonAsync<AccountDto>(_jsonOptions);

        // Act
        var response = await _client.PostAsync($"/api/accounts/{createdAccount!.Id}/close", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify account is closed
        var getResponse = await _client.GetAsync($"/api/accounts/{createdAccount.Id}");
        var account = await getResponse.Content.ReadFromJsonAsync<AccountDto>(_jsonOptions);
        Assert.Equal(AccountStatus.Closed, account!.Status);
        Assert.NotNull(account.ClosedDate);
    }

    [Fact]
    public async Task CloseAccount_NonExistingAccount_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsync($"/api/accounts/{Guid.NewGuid()}/close", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Get Transactions Tests

    [Fact]
    public async Task GetTransactions_NoTransactions_ReturnsEmptyList()
    {
        // Arrange
        var createRequest = new
        {
            AccountHolderName = "John Doe",
            Type = AccountType.Checking,
            CurrencyCode = "USD"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/accounts", createRequest);
        var createdAccount = await createResponse.Content.ReadFromJsonAsync<AccountDto>(_jsonOptions);

        // Act
        var response = await _client.GetAsync($"/api/accounts/{createdAccount!.Id}/transactions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"items\":[]", content);
        Assert.Contains("\"totalCount\":0", content);
    }

    #endregion
}
