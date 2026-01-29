namespace Application.Accounts.DTOs;

public record AccountBalanceDto
{
    public Guid AccountId { get; init; }
    public string AccountNumber { get; init; } = null!;
    public decimal Balance { get; init; }
    public string CurrencyCode { get; init; } = null!;
    public DateTime AsOf { get; init; }
}
