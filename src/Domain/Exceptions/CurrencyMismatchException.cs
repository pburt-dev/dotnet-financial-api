namespace Domain.Exceptions;

public class CurrencyMismatchException : DomainException
{
    public string ExpectedCurrency { get; }
    public string ActualCurrency { get; }

    public CurrencyMismatchException(string expectedCurrency, string actualCurrency)
        : base($"Currency mismatch. Expected: {expectedCurrency}, Actual: {actualCurrency}")
    {
        ExpectedCurrency = expectedCurrency;
        ActualCurrency = actualCurrency;
    }
}
