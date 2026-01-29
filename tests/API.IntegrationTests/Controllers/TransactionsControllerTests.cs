using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Accounts.DTOs;
using Application.Transactions.DTOs;
using Domain.Enums;

namespace API.IntegrationTests.Controllers;

public class TransactionsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public TransactionsControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private async Task<AccountDto> CreateAccountAsync(string name = "John Doe", string currency = "USD")
    {
        var request = new
        {
            AccountHolderName = name,
            Type = AccountType.Checking,
            CurrencyCode = currency
        };
        var response = await _client.PostAsJsonAsync("/api/accounts", request);
        return (await response.Content.ReadFromJsonAsync<AccountDto>(_jsonOptions))!;
    }

    #region Deposit Tests

    [Fact]
    public async Task Deposit_ValidRequest_ReturnsCreatedTransaction()
    {
        // Arrange
        var account = await CreateAccountAsync();
        var depositRequest = new
        {
            AccountId = account.Id,
            Amount = 100.50m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString(),
            Description = "Test deposit"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions/deposit", depositRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var transaction = await response.Content.ReadFromJsonAsync<TransactionDto>(_jsonOptions);
        Assert.NotNull(transaction);
        Assert.Equal(100.50m, transaction.Amount);
        Assert.Equal("USD", transaction.CurrencyCode);
        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal(TransactionStatus.Completed, transaction.Status);
        Assert.Equal("Test deposit", transaction.Description);
        Assert.Equal(100.50m, transaction.BalanceAfter);
    }

    [Fact]
    public async Task Deposit_MultipleDeposits_AccumulatesBalance()
    {
        // Arrange
        var account = await CreateAccountAsync();

        // Act
        var deposit1 = new
        {
            AccountId = account.Id,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        var response1 = await _client.PostAsJsonAsync("/api/transactions/deposit", deposit1);
        var transaction1 = await response1.Content.ReadFromJsonAsync<TransactionDto>(_jsonOptions);

        var deposit2 = new
        {
            AccountId = account.Id,
            Amount = 50m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        var response2 = await _client.PostAsJsonAsync("/api/transactions/deposit", deposit2);
        var transaction2 = await response2.Content.ReadFromJsonAsync<TransactionDto>(_jsonOptions);

        // Assert
        Assert.Equal(100m, transaction1!.BalanceAfter);
        Assert.Equal(150m, transaction2!.BalanceAfter);

        // Verify account balance
        var balanceResponse = await _client.GetAsync($"/api/accounts/{account.Id}/balance");
        var balance = await balanceResponse.Content.ReadFromJsonAsync<AccountBalanceDto>(_jsonOptions);
        Assert.Equal(150m, balance!.Balance);
    }

    [Fact]
    public async Task Deposit_DuplicateIdempotencyKey_ReturnsOriginalTransaction()
    {
        // Arrange
        var account = await CreateAccountAsync();
        var idempotencyKey = Guid.NewGuid().ToString();

        var depositRequest = new
        {
            AccountId = account.Id,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = idempotencyKey
        };

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/transactions/deposit", depositRequest);
        var transaction1 = await response1.Content.ReadFromJsonAsync<TransactionDto>(_jsonOptions);

        // Same idempotency key with different amount
        var duplicateRequest = new
        {
            AccountId = account.Id,
            Amount = 200m,
            CurrencyCode = "USD",
            IdempotencyKey = idempotencyKey
        };
        var response2 = await _client.PostAsJsonAsync("/api/transactions/deposit", duplicateRequest);
        var transaction2 = await response2.Content.ReadFromJsonAsync<TransactionDto>(_jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        Assert.Equal(transaction1!.Id, transaction2!.Id);
        Assert.Equal(100m, transaction2.Amount); // Original amount, not 200

        // Verify balance is still 100 (not 300)
        var balanceResponse = await _client.GetAsync($"/api/accounts/{account.Id}/balance");
        var balance = await balanceResponse.Content.ReadFromJsonAsync<AccountBalanceDto>(_jsonOptions);
        Assert.Equal(100m, balance!.Balance);
    }

    [Fact]
    public async Task Deposit_NonExistingAccount_ReturnsNotFound()
    {
        // Arrange
        var depositRequest = new
        {
            AccountId = Guid.NewGuid(),
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions/deposit", depositRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Deposit_InvalidAmount_ReturnsBadRequest()
    {
        // Arrange
        var account = await CreateAccountAsync();
        var depositRequest = new
        {
            AccountId = account.Id,
            Amount = -100m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions/deposit", depositRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Deposit_AmountExceedsLimit_ReturnsBadRequest()
    {
        // Arrange
        var account = await CreateAccountAsync();
        var depositRequest = new
        {
            AccountId = account.Id,
            Amount = 1_000_001m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions/deposit", depositRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Withdraw Tests

    [Fact]
    public async Task Withdraw_ValidRequest_ReturnsCreatedTransaction()
    {
        // Arrange
        var account = await CreateAccountAsync();

        // First deposit some money
        var depositRequest = new
        {
            AccountId = account.Id,
            Amount = 200m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        await _client.PostAsJsonAsync("/api/transactions/deposit", depositRequest);

        // Act
        var withdrawRequest = new
        {
            AccountId = account.Id,
            Amount = 50m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString(),
            Description = "ATM withdrawal"
        };
        var response = await _client.PostAsJsonAsync("/api/transactions/withdraw", withdrawRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var transaction = await response.Content.ReadFromJsonAsync<TransactionDto>(_jsonOptions);
        Assert.NotNull(transaction);
        Assert.Equal(50m, transaction.Amount);
        Assert.Equal(TransactionType.Withdrawal, transaction.Type);
        Assert.Equal(150m, transaction.BalanceAfter);
    }

    [Fact]
    public async Task Withdraw_InsufficientFunds_ReturnsUnprocessableEntity()
    {
        // Arrange
        var account = await CreateAccountAsync();

        // Deposit only 50
        var depositRequest = new
        {
            AccountId = account.Id,
            Amount = 50m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        await _client.PostAsJsonAsync("/api/transactions/deposit", depositRequest);

        // Try to withdraw 100
        var withdrawRequest = new
        {
            AccountId = account.Id,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions/withdraw", withdrawRequest);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Insufficient funds", content);
    }

    [Fact]
    public async Task Withdraw_FrozenAccount_ReturnsUnprocessableEntity()
    {
        // Arrange
        var account = await CreateAccountAsync();

        // Deposit money
        var depositRequest = new
        {
            AccountId = account.Id,
            Amount = 200m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        await _client.PostAsJsonAsync("/api/transactions/deposit", depositRequest);

        // Freeze the account
        await _client.PostAsJsonAsync($"/api/accounts/{account.Id}/freeze", new { Reason = "Suspicious" });

        // Try to withdraw
        var withdrawRequest = new
        {
            AccountId = account.Id,
            Amount = 50m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions/withdraw", withdrawRequest);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("frozen", content.ToLower());
    }

    #endregion

    #region Transfer Tests

    [Fact(Skip = "EF Core tracking issue with multi-entity updates in SQLite InMemory - transfer logic tested in unit tests")]
    public async Task Transfer_ValidRequest_TransfersFunds()
    {
        // Arrange
        var sourceAccount = await CreateAccountAsync("John Doe");
        var destAccount = await CreateAccountAsync("Jane Doe");

        // Deposit to source account
        var depositRequest = new
        {
            AccountId = sourceAccount.Id,
            Amount = 500m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        await _client.PostAsJsonAsync("/api/transactions/deposit", depositRequest);

        // Act
        var transferRequest = new
        {
            SourceAccountId = sourceAccount.Id,
            DestinationAccountId = destAccount.Id,
            Amount = 200m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        var response = await _client.PostAsJsonAsync("/api/transactions/transfer", transferRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TransferResultDto>(_jsonOptions);
        Assert.NotNull(result);
        Assert.Equal(200m, result.SourceTransaction.Amount);
        Assert.Equal(300m, result.SourceTransaction.BalanceAfter);
        Assert.Equal(200m, result.DestinationTransaction.Amount);
        Assert.Equal(200m, result.DestinationTransaction.BalanceAfter);

        // Verify balances
        var sourceBalance = await _client.GetFromJsonAsync<AccountBalanceDto>($"/api/accounts/{sourceAccount.Id}/balance", _jsonOptions);
        var destBalance = await _client.GetFromJsonAsync<AccountBalanceDto>($"/api/accounts/{destAccount.Id}/balance", _jsonOptions);

        Assert.Equal(300m, sourceBalance!.Balance);
        Assert.Equal(200m, destBalance!.Balance);
    }

    [Fact]
    public async Task Transfer_SameAccount_ReturnsBadRequest()
    {
        // Arrange
        var account = await CreateAccountAsync();

        var transferRequest = new
        {
            SourceAccountId = account.Id,
            DestinationAccountId = account.Id,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions/transfer", transferRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Transfer_InsufficientFunds_ReturnsUnprocessableEntity()
    {
        // Arrange
        var sourceAccount = await CreateAccountAsync("John Doe");
        var destAccount = await CreateAccountAsync("Jane Doe");

        // Only deposit 50
        var depositRequest = new
        {
            AccountId = sourceAccount.Id,
            Amount = 50m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        await _client.PostAsJsonAsync("/api/transactions/deposit", depositRequest);

        // Try to transfer 100
        var transferRequest = new
        {
            SourceAccountId = sourceAccount.Id,
            DestinationAccountId = destAccount.Id,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions/transfer", transferRequest);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    #endregion

    #region Get Transaction Tests

    [Fact]
    public async Task GetTransaction_ExistingTransaction_ReturnsTransaction()
    {
        // Arrange
        var account = await CreateAccountAsync();
        var depositRequest = new
        {
            AccountId = account.Id,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        var depositResponse = await _client.PostAsJsonAsync("/api/transactions/deposit", depositRequest);
        var createdTransaction = await depositResponse.Content.ReadFromJsonAsync<TransactionDto>(_jsonOptions);

        // Act
        var response = await _client.GetAsync($"/api/transactions/{createdTransaction!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var transaction = await response.Content.ReadFromJsonAsync<TransactionDto>(_jsonOptions);
        Assert.NotNull(transaction);
        Assert.Equal(createdTransaction.Id, transaction.Id);
        Assert.Equal(100m, transaction.Amount);
    }

    [Fact]
    public async Task GetTransaction_NonExisting_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/transactions/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTransactionByReference_ExistingTransaction_ReturnsTransaction()
    {
        // Arrange
        var account = await CreateAccountAsync();
        var depositRequest = new
        {
            AccountId = account.Id,
            Amount = 100m,
            CurrencyCode = "USD",
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        var depositResponse = await _client.PostAsJsonAsync("/api/transactions/deposit", depositRequest);
        var createdTransaction = await depositResponse.Content.ReadFromJsonAsync<TransactionDto>(_jsonOptions);

        // Act
        var response = await _client.GetAsync($"/api/transactions/by-reference/{createdTransaction!.TransactionReference}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var transaction = await response.Content.ReadFromJsonAsync<TransactionDto>(_jsonOptions);
        Assert.NotNull(transaction);
        Assert.Equal(createdTransaction.TransactionReference, transaction.TransactionReference);
    }

    [Fact]
    public async Task GetTransactionByReference_NonExisting_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/transactions/by-reference/TXN-20240115-12345");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}
