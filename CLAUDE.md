# Financial Transaction API - Portfolio Project

## Project Overview

Build a production-grade Financial Transaction API demonstrating enterprise .NET patterns. This project showcases skills relevant to financial services (AmEx, banks), enterprise software (Microsoft), and secure systems (defense contractors).

**Target Audience:** Technical reviewers, hiring managers, potential consulting clients

**Key Differentiators:**
- Clean Architecture with proper separation of concerns
- Proper decimal/money handling (no floating point for currency)
- Comprehensive audit logging
- Idempotency for safe retries
- Role-based access control
- Unit and integration tests

---

## Technology Stack

- **.NET 8** (latest LTS)
- **ASP.NET Core Web API**
- **Entity Framework Core 8** with SQL Server
- **MediatR** for CQRS pattern
- **FluentValidation** for request validation
- **Serilog** for structured logging
- **xUnit + Moq** for testing
- **Docker Compose** for local development
- **Swagger/OpenAPI** for documentation

---

## Solution Structure

```
FinancialTransactionAPI/
├── src/
│   ├── API/                          # Presentation layer
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Filters/
│   │   └── Program.cs
│   ├── Application/                  # Business logic layer
│   │   ├── Common/
│   │   │   ├── Behaviors/            # MediatR pipeline behaviors
│   │   │   ├── Interfaces/
│   │   │   └── Models/
│   │   ├── Accounts/
│   │   │   ├── Commands/
│   │   │   ├── Queries/
│   │   │   └── DTOs/
│   │   └── Transactions/
│   │       ├── Commands/
│   │       ├── Queries/
│   │       └── DTOs/
│   ├── Domain/                       # Enterprise business rules
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Enums/
│   │   ├── Events/
│   │   └── Exceptions/
│   └── Infrastructure/               # External concerns
│       ├── Persistence/
│       │   ├── Configurations/
│       │   ├── Repositories/
│       │   └── ApplicationDbContext.cs
│       ├── Services/
│       └── DependencyInjection.cs
├── tests/
│   ├── Domain.UnitTests/
│   ├── Application.UnitTests/
│   └── API.IntegrationTests/
├── docker-compose.yml
├── README.md
└── CLAUDE.md                         # This file
```

---

## Domain Model

### Entities

#### Account
```csharp
public class Account : BaseAuditableEntity
{
    public Guid Id { get; private set; }
    public string AccountNumber { get; private set; }  // Format: XXXX-XXXX-XXXX
    public string AccountHolderName { get; private set; }
    public Money Balance { get; private set; }
    public AccountStatus Status { get; private set; }
    public AccountType Type { get; private set; }
    public DateTime OpenedDate { get; private set; }
    public DateTime? ClosedDate { get; private set; }
    
    private readonly List<Transaction> _transactions = new();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();
    
    // Domain methods
    public void Deposit(Money amount, string idempotencyKey);
    public void Withdraw(Money amount, string idempotencyKey);
    public void Freeze(string reason);
    public void Unfreeze();
    public void Close();
}
```

#### Transaction
```csharp
public class Transaction : BaseAuditableEntity
{
    public Guid Id { get; private set; }
    public string TransactionReference { get; private set; }  // Unique reference
    public Guid AccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; }
    public Money BalanceAfter { get; private set; }
    public TransactionStatus Status { get; private set; }
    public string Description { get; private set; }
    public string IdempotencyKey { get; private set; }  // For safe retries
    public DateTime ProcessedAt { get; private set; }
    
    // For transfers
    public Guid? CounterpartyAccountId { get; private set; }
}
```

### Value Objects

#### Money (CRITICAL - Never use decimal directly for currency)
```csharp
public record Money
{
    public decimal Amount { get; }
    public string CurrencyCode { get; }  // ISO 4217: USD, EUR, etc.
    
    public Money(decimal amount, string currencyCode)
    {
        if (amount < 0) throw new DomainException("Amount cannot be negative");
        Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);  // Banker's rounding
        CurrencyCode = currencyCode?.ToUpperInvariant() 
            ?? throw new ArgumentNullException(nameof(currencyCode));
    }
    
    public static Money Zero(string currency) => new(0, currency);
    public static Money USD(decimal amount) => new(amount, "USD");
    
    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, CurrencyCode);
    }
    
    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        if (other.Amount > Amount) 
            throw new InsufficientFundsException(this, other);
        return new Money(Amount - other.Amount, CurrencyCode);
    }
}
```

### Enums

```csharp
public enum AccountStatus { Active, Frozen, Closed }
public enum AccountType { Checking, Savings, Investment }
public enum TransactionType { Deposit, Withdrawal, Transfer, Fee, Interest }
public enum TransactionStatus { Pending, Completed, Failed, Reversed }
```

---

## API Endpoints

### Accounts

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/accounts` | Create new account |
| GET | `/api/accounts/{id}` | Get account details |
| GET | `/api/accounts/{id}/balance` | Get current balance |
| GET | `/api/accounts/{id}/transactions` | Get transaction history (paginated) |
| POST | `/api/accounts/{id}/freeze` | Freeze account |
| POST | `/api/accounts/{id}/unfreeze` | Unfreeze account |
| POST | `/api/accounts/{id}/close` | Close account |

### Transactions

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/transactions/deposit` | Deposit funds |
| POST | `/api/transactions/withdraw` | Withdraw funds |
| POST | `/api/transactions/transfer` | Transfer between accounts |
| GET | `/api/transactions/{id}` | Get transaction details |
| GET | `/api/transactions/by-reference/{ref}` | Get by reference number |

---

## Key Implementation Details

### 1. Idempotency (CRITICAL for financial systems)

Every mutation request must include an `Idempotency-Key` header. If the same key is seen twice, return the original response without re-executing.

```csharp
// Middleware approach
public class IdempotencyMiddleware
{
    public async Task InvokeAsync(HttpContext context, IIdempotencyService service)
    {
        if (!IsIdempotentMethod(context.Request.Method)) 
        {
            await _next(context);
            return;
        }
        
        var key = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(key))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Idempotency-Key header required" });
            return;
        }
        
        var cached = await service.GetCachedResponseAsync(key);
        if (cached != null)
        {
            context.Response.StatusCode = cached.StatusCode;
            await context.Response.WriteAsync(cached.Body);
            return;
        }
        
        // Capture and cache response
        // ...
    }
}
```

### 2. Audit Logging

Every entity change must be logged with who, what, when:

```csharp
public abstract class BaseAuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string LastModifiedBy { get; set; }
}

// In DbContext
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedBy = _currentUserService.UserId;
                break;
            case EntityState.Modified:
                entry.Entity.LastModifiedAt = DateTime.UtcNow;
                entry.Entity.LastModifiedBy = _currentUserService.UserId;
                break;
        }
    }
    return base.SaveChangesAsync(cancellationToken);
}
```

### 3. Validation with FluentValidation

```csharp
public class DepositCommandValidator : AbstractValidator<DepositCommand>
{
    public DepositCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required");
            
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be positive")
            .LessThanOrEqualTo(1_000_000).WithMessage("Single deposit cannot exceed $1,000,000");
            
        RuleFor(x => x.CurrencyCode)
            .NotEmpty()
            .Must(BeValidCurrency).WithMessage("Invalid currency code");
            
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required")
            .MaximumLength(64);
    }
    
    private bool BeValidCurrency(string code) => 
        new[] { "USD", "EUR", "GBP" }.Contains(code?.ToUpperInvariant());
}
```

### 4. MediatR Pipeline Behaviors

```csharp
// Validation behavior - runs before handler
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();
            
        if (failures.Any())
            throw new ValidationException(failures);
            
        return await next();
    }
}

// Logging behavior
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {RequestName} {@Request}", typeof(TRequest).Name, request);
        
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();
        
        _logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);
        
        return response;
    }
}
```

### 5. Exception Handling Middleware

```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException ex => (400, new { errors = ex.Errors }),
            NotFoundException ex => (404, new { error = ex.Message }),
            InsufficientFundsException ex => (422, new { error = ex.Message, available = ex.Available, requested = ex.Requested }),
            AccountFrozenException ex => (422, new { error = "Account is frozen", reason = ex.Reason }),
            DuplicateIdempotencyKeyException => (409, new { error = "Request already processed" }),
            _ => (500, new { error = "An unexpected error occurred" })
        };
        
        _logger.LogError(exception, "Exception caught: {Message}", exception.Message);
        
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(response);
    }
}
```

---

## Testing Requirements

### Domain Unit Tests
- Money value object arithmetic
- Account state transitions (freeze, unfreeze, close)
- Balance calculations after transactions
- Insufficient funds scenarios
- Currency mismatch handling

### Application Unit Tests  
- Command/Query handlers with mocked repositories
- Validation rules
- Business logic edge cases

### Integration Tests
- Full API endpoint tests with in-memory database
- Idempotency behavior verification
- Concurrent transaction handling
- Error response formats

---

## Docker Setup

```yaml
# docker-compose.yml
version: '3.8'
services:
  api:
    build: .
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=db;Database=FinancialTransactions;User=sa;Password=YourStrong!Password;TrustServerCertificate=true
    depends_on:
      - db
      
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong!Password
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql

volumes:
  sqldata:
```

---

## README Requirements

The README.md should include:
1. Project overview and purpose
2. Architecture diagram (use Mermaid)
3. How to run locally (Docker and direct)
4. API documentation link (Swagger)
5. Design decisions explained
6. Testing instructions
7. Future enhancements section

---

## Implementation Order

**Phase 1: Foundation**
1. Create solution structure with all projects
2. Set up Domain layer (entities, value objects, exceptions)
3. Set up Infrastructure layer (DbContext, configurations)
4. Set up API layer (Program.cs, middleware)

**Phase 2: Core Features**
1. Implement Account CRUD operations
2. Implement Transaction operations (deposit, withdraw)
3. Add validation pipeline
4. Add audit logging

**Phase 3: Advanced Features**
1. Implement idempotency middleware
2. Add transfer functionality
3. Implement pagination for transaction history
4. Add comprehensive error handling

**Phase 4: Quality**
1. Write domain unit tests
2. Write application unit tests
3. Write integration tests
4. Add Swagger documentation with examples

**Phase 5: Polish**
1. Create comprehensive README
2. Add Docker support
3. Add sample seed data
4. Final code review and cleanup

---

## Code Style Guidelines

- Use nullable reference types (`<Nullable>enable</Nullable>`)
- Use file-scoped namespaces
- Use primary constructors where appropriate
- Prefer records for DTOs and value objects
- Use meaningful names (no abbreviations)
- XML documentation on public APIs
- One class per file

---

## Commands for Claude Code

Here are example prompts you can use:

**Start the project:**
```
Create the .NET 8 solution with the folder structure defined in this CLAUDE.md. 
Set up all four projects (API, Application, Domain, Infrastructure) with proper 
project references and NuGet packages.
```

**Build domain layer:**
```
Implement the Domain layer with the Account entity, Transaction entity, 
Money value object, and all enums exactly as specified in CLAUDE.md. 
Include proper encapsulation and domain methods.
```

**Add tests:**
```
Write comprehensive xUnit tests for the Money value object covering:
- Basic arithmetic (add, subtract)
- Currency validation
- Banker's rounding
- Insufficient funds exception
- Currency mismatch handling
```

**Run parallel tasks:**
```
I need three things done in parallel:
1. Implement the DepositCommand and handler
2. Implement the WithdrawCommand and handler  
3. Write integration tests for both endpoints
Use three agents for this.
```
