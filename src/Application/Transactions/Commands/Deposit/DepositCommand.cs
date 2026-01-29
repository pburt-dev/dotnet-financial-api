using Application.Transactions.DTOs;
using MediatR;

namespace Application.Transactions.Commands.Deposit;

public record DepositCommand : IRequest<TransactionDto>
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public string IdempotencyKey { get; init; } = null!;
    public string? Description { get; init; }
}
