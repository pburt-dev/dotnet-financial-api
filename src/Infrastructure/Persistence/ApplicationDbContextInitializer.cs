using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

/// <summary>
/// Initializes the database with migrations and optional seed data.
/// </summary>
public class ApplicationDbContextInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApplicationDbContextInitializer> _logger;

    public ApplicationDbContextInitializer(
        ApplicationDbContext context,
        ILogger<ApplicationDbContextInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Ensures the database is created and applies any pending migrations.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Check if there are pending migrations
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                _logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
                await _context.Database.MigrateAsync();
            }
            else
            {
                // No migrations exist, ensure database is created
                _logger.LogInformation("No migrations found. Ensuring database is created...");
                await _context.Database.EnsureCreatedAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }

    /// <summary>
    /// Seeds the database with sample data for demonstration purposes.
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task TrySeedAsync()
    {
        // Skip seeding if data already exists
        if (await _context.Accounts.AnyAsync())
        {
            _logger.LogInformation("Database already contains data. Skipping seed.");
            return;
        }

        _logger.LogInformation("Seeding database with sample data...");

        // Create sample accounts
        var johnChecking = Account.Create("John Smith", AccountType.Checking, "USD");
        var johnSavings = Account.Create("John Smith", AccountType.Savings, "USD");
        var janeChecking = Account.Create("Jane Doe", AccountType.Checking, "USD");
        var janeInvestment = Account.Create("Jane Doe", AccountType.Investment, "USD");
        var corporateAccount = Account.Create("Acme Corporation", AccountType.Checking, "USD");

        // Add initial deposits to accounts
        johnChecking.Deposit(Money.USD(5000.00m), "seed-john-checking-001", "Initial deposit");
        johnChecking.Deposit(Money.USD(2500.00m), "seed-john-checking-002", "Salary deposit");

        johnSavings.Deposit(Money.USD(10000.00m), "seed-john-savings-001", "Initial deposit");
        johnSavings.Deposit(Money.USD(500.00m), "seed-john-savings-002", "Monthly savings transfer");

        janeChecking.Deposit(Money.USD(3500.00m), "seed-jane-checking-001", "Initial deposit");
        janeChecking.Deposit(Money.USD(1200.00m), "seed-jane-checking-002", "Freelance payment");

        janeInvestment.Deposit(Money.USD(25000.00m), "seed-jane-investment-001", "Initial investment");

        corporateAccount.Deposit(Money.USD(100000.00m), "seed-corp-001", "Initial business capital");
        corporateAccount.Deposit(Money.USD(45000.00m), "seed-corp-002", "Client payment - Project Alpha");

        // Add some withdrawals
        johnChecking.Withdraw(Money.USD(150.00m), "seed-john-checking-w001", "ATM withdrawal");
        johnChecking.Withdraw(Money.USD(89.99m), "seed-john-checking-w002", "Online purchase");

        janeChecking.Withdraw(Money.USD(200.00m), "seed-jane-checking-w001", "Grocery shopping");

        corporateAccount.Withdraw(Money.USD(5000.00m), "seed-corp-w001", "Equipment purchase");
        corporateAccount.Withdraw(Money.USD(12500.00m), "seed-corp-w002", "Vendor payment");

        // Add a transfer between accounts
        johnChecking.TransferOut(Money.USD(500.00m), johnSavings.Id, "seed-transfer-001");
        johnSavings.TransferIn(Money.USD(500.00m), johnChecking.Id, "seed-transfer-001");

        // Add accounts to context
        _context.Accounts.AddRange(johnChecking, johnSavings, janeChecking, janeInvestment, corporateAccount);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Database seeded successfully with {Count} accounts.", 5);
    }
}
