using Application.Transactions.DTOs;
using MediatR;

namespace Application.Transactions.Commands.Transfer;

public record TransferCommand : IRequest<TransferResultDto>
{
    public Guid SourceAccountId { get; init; }
    public Guid DestinationAccountId { get; init; }
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public string IdempotencyKey { get; init; } = null!;
}
