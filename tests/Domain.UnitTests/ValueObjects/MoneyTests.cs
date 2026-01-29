using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.UnitTests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_WithValidAmount_CreatesMoney()
    {
        var money = new Money(100.50m, "USD");

        Assert.Equal(100.50m, money.Amount);
        Assert.Equal("USD", money.CurrencyCode);
    }

    [Fact]
    public void Constructor_WithLowercaseCurrency_ConvertsToUppercase()
    {
        var money = new Money(100m, "usd");

        Assert.Equal("USD", money.CurrencyCode);
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new Money(-100m, "USD"));

        Assert.Equal("Amount cannot be negative", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullCurrency_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Money(100m, null!));
    }

    [Fact]
    public void Constructor_WithEmptyCurrency_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new Money(100m, ""));

        Assert.Equal("Currency code cannot be empty", exception.Message);
    }

    [Theory]
    [InlineData(10.555, 10.56)] // Rounds up (banker's rounding - 5 rounds to even)
    [InlineData(10.545, 10.54)] // Rounds down (banker's rounding - 5 rounds to even)
    [InlineData(10.5451, 10.55)] // Rounds up
    [InlineData(10.5449, 10.54)] // Rounds down
    [InlineData(10.125, 10.12)] // Banker's rounding: .125 -> .12 (rounds to even)
    [InlineData(10.135, 10.14)] // Banker's rounding: .135 -> .14 (rounds to even)
    public void Constructor_AppliesBankersRounding(decimal input, decimal expected)
    {
        var money = new Money(input, "USD");

        Assert.Equal(expected, money.Amount);
    }

    [Fact]
    public void Add_WithSameCurrency_ReturnsCorrectSum()
    {
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(50.25m, "USD");

        var result = money1.Add(money2);

        Assert.Equal(150.75m, result.Amount);
        Assert.Equal("USD", result.CurrencyCode);
    }

    [Fact]
    public void Add_WithDifferentCurrency_ThrowsCurrencyMismatchException()
    {
        var money1 = new Money(100m, "USD");
        var money2 = new Money(50m, "EUR");

        var exception = Assert.Throws<CurrencyMismatchException>(() => money1.Add(money2));

        Assert.Equal("USD", exception.ExpectedCurrency);
        Assert.Equal("EUR", exception.ActualCurrency);
    }

    [Fact]
    public void Subtract_WithSameCurrency_ReturnsCorrectDifference()
    {
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(50.25m, "USD");

        var result = money1.Subtract(money2);

        Assert.Equal(50.25m, result.Amount);
        Assert.Equal("USD", result.CurrencyCode);
    }

    [Fact]
    public void Subtract_WithDifferentCurrency_ThrowsCurrencyMismatchException()
    {
        var money1 = new Money(100m, "USD");
        var money2 = new Money(50m, "EUR");

        var exception = Assert.Throws<CurrencyMismatchException>(() => money1.Subtract(money2));

        Assert.Equal("USD", exception.ExpectedCurrency);
        Assert.Equal("EUR", exception.ActualCurrency);
    }

    [Fact]
    public void Subtract_WhenInsufficientFunds_ThrowsInsufficientFundsException()
    {
        var money1 = new Money(50m, "USD");
        var money2 = new Money(100m, "USD");

        var exception = Assert.Throws<InsufficientFundsException>(() => money1.Subtract(money2));

        Assert.Equal(50m, exception.Available.Amount);
        Assert.Equal(100m, exception.Requested.Amount);
    }

    [Fact]
    public void Multiply_WithPositiveFactor_ReturnsCorrectProduct()
    {
        var money = new Money(100m, "USD");

        var result = money.Multiply(1.5m);

        Assert.Equal(150m, result.Amount);
        Assert.Equal("USD", result.CurrencyCode);
    }

    [Fact]
    public void Multiply_WithNegativeFactor_ThrowsDomainException()
    {
        var money = new Money(100m, "USD");

        var exception = Assert.Throws<DomainException>(() => money.Multiply(-1m));

        Assert.Equal("Cannot multiply money by a negative factor", exception.Message);
    }

    [Fact]
    public void IsGreaterThan_WhenGreater_ReturnsTrue()
    {
        var money1 = new Money(100m, "USD");
        var money2 = new Money(50m, "USD");

        Assert.True(money1.IsGreaterThan(money2));
    }

    [Fact]
    public void IsGreaterThan_WhenEqual_ReturnsFalse()
    {
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "USD");

        Assert.False(money1.IsGreaterThan(money2));
    }

    [Fact]
    public void IsGreaterThan_WhenLess_ReturnsFalse()
    {
        var money1 = new Money(50m, "USD");
        var money2 = new Money(100m, "USD");

        Assert.False(money1.IsGreaterThan(money2));
    }

    [Fact]
    public void IsLessThan_WhenLess_ReturnsTrue()
    {
        var money1 = new Money(50m, "USD");
        var money2 = new Money(100m, "USD");

        Assert.True(money1.IsLessThan(money2));
    }

    [Fact]
    public void IsZero_WhenZero_ReturnsTrue()
    {
        var money = Money.Zero("USD");

        Assert.True(money.IsZero);
    }

    [Fact]
    public void IsZero_WhenNotZero_ReturnsFalse()
    {
        var money = new Money(100m, "USD");

        Assert.False(money.IsZero);
    }

    [Fact]
    public void Zero_CreatesMoneyWithZeroAmount()
    {
        var money = Money.Zero("EUR");

        Assert.Equal(0m, money.Amount);
        Assert.Equal("EUR", money.CurrencyCode);
    }

    [Fact]
    public void USD_CreatesMoneyWithUSDCurrency()
    {
        var money = Money.USD(100m);

        Assert.Equal(100m, money.Amount);
        Assert.Equal("USD", money.CurrencyCode);
    }

    [Fact]
    public void EUR_CreatesMoneyWithEURCurrency()
    {
        var money = Money.EUR(100m);

        Assert.Equal(100m, money.Amount);
        Assert.Equal("EUR", money.CurrencyCode);
    }

    [Fact]
    public void GBP_CreatesMoneyWithGBPCurrency()
    {
        var money = Money.GBP(100m);

        Assert.Equal(100m, money.Amount);
        Assert.Equal("GBP", money.CurrencyCode);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var money = new Money(1234.56m, "USD");

        Assert.Equal("1234.56 USD", money.ToString());
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "USD");

        Assert.Equal(money1, money2);
    }

    [Fact]
    public void Equality_DifferentAmounts_AreNotEqual()
    {
        var money1 = new Money(100m, "USD");
        var money2 = new Money(200m, "USD");

        Assert.NotEqual(money1, money2);
    }

    [Fact]
    public void Equality_DifferentCurrencies_AreNotEqual()
    {
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "EUR");

        Assert.NotEqual(money1, money2);
    }
}
