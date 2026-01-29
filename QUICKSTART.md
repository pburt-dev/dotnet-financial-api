# Quick Start Guide

## How to Use This Spec with Claude Code

### Step 1: Set Up Your Environment

```bash
# Install Claude Code (pick one)
brew install claude-code          # macOS
winget install Anthropic.ClaudeCode  # Windows

# Create your project folder
mkdir FinancialTransactionAPI
cd FinancialTransactionAPI

# Copy the CLAUDE.md file into this folder
# (Claude Code automatically reads CLAUDE.md for context)
```

### Step 2: Start Claude Code

```bash
claude
```

### Step 3: Build the Project

Copy and paste these prompts in sequence:

**Prompt 1 - Create Solution Structure:**
```
Create the .NET 8 solution with the folder structure defined in CLAUDE.md. 
Set up all four projects (API, Application, Domain, Infrastructure) with 
proper project references. Add these NuGet packages:
- API: Serilog.AspNetCore, Swashbuckle.AspNetCore
- Application: MediatR, FluentValidation, FluentValidation.DependencyInjectionExtensions
- Infrastructure: Microsoft.EntityFrameworkCore.SqlServer, Microsoft.EntityFrameworkCore.Tools
- Tests: xUnit, Moq, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing
```

**Prompt 2 - Build Domain Layer:**
```
Implement the complete Domain layer as specified in CLAUDE.md:
- Money value object with proper arithmetic and banker's rounding
- Account entity with all domain methods (Deposit, Withdraw, Freeze, Unfreeze, Close)
- Transaction entity
- All enums (AccountStatus, AccountType, TransactionType, TransactionStatus)
- Custom domain exceptions (DomainException, InsufficientFundsException, AccountFrozenException)
- BaseAuditableEntity base class
```

**Prompt 3 - Build Infrastructure Layer:**
```
Implement the Infrastructure layer:
- ApplicationDbContext with audit logging in SaveChangesAsync
- Entity configurations for Account and Transaction using Fluent API
- Money value object conversion for EF Core
- Generic repository interface and implementation
- DependencyInjection.cs to register all services
```

**Prompt 4 - Build Application Layer (use subagents):**
```
I need three things done in parallel using three agents:
1. Create Account commands (CreateAccount) and queries (GetAccount, GetAccountBalance) with MediatR handlers
2. Create Transaction commands (Deposit, Withdraw) with MediatR handlers
3. Create DTOs for all requests/responses and FluentValidation validators
```

**Prompt 5 - Build API Layer:**
```
Implement the API layer:
- AccountsController with all endpoints from CLAUDE.md
- TransactionsController with all endpoints
- GlobalExceptionMiddleware for consistent error responses
- IdempotencyMiddleware that checks for Idempotency-Key header
- Program.cs with all service registrations and middleware pipeline
- Swagger configuration with examples
```

**Prompt 6 - Add Tests (use subagents):**
```
Create comprehensive tests in parallel:
1. Domain.UnitTests: Test Money value object and Account entity methods
2. Application.UnitTests: Test command handlers with mocked repositories
3. API.IntegrationTests: Test full API endpoints with WebApplicationFactory
Use three agents.
```

**Prompt 7 - Add Docker and Documentation:**
```
Add Docker support and documentation:
- Dockerfile for the API project
- docker-compose.yml as specified in CLAUDE.md
- Comprehensive README.md with architecture diagram (Mermaid), 
  setup instructions, and API documentation
- Add seed data for demo purposes
```

### Step 4: Run and Test

```bash
# Start the database
docker-compose up -d db

# Run the API
cd src/API
dotnet run

# Or run everything in Docker
docker-compose up --build
```

### Tips for Best Results

1. **Use `/clear` between major phases** - Keeps context focused
2. **Ask Claude to explain decisions** - "Why did you structure it this way?"
3. **Request code review** - "Review the Account entity for any DDD violations"
4. **Run tests frequently** - "Run all tests and fix any failures"
5. **Use subagents for independent tasks** - Speeds up development significantly

### Customization Ideas

Once the base project is working, consider asking Claude to add:

- **JWT Authentication** - "Add JWT authentication with role-based authorization"
- **Rate Limiting** - "Add rate limiting middleware with configurable limits per endpoint"
- **Caching** - "Add Redis caching for account balance queries"
- **Event Sourcing** - "Refactor to use event sourcing for the Transaction aggregate"
- **GraphQL** - "Add a GraphQL endpoint alongside the REST API"

### Troubleshooting

If Claude seems confused or off-track:
1. Use `/clear` to reset context
2. Reference specific sections: "Look at the Money value object section in CLAUDE.md"
3. Break down complex requests into smaller steps
4. Ask Claude to read the CLAUDE.md file: "Re-read CLAUDE.md and summarize the domain model"
