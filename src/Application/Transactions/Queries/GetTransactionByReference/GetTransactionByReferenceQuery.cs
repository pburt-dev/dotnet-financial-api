using Application.Transactions.DTOs;
using MediatR;

namespace Application.Transactions.Queries.GetTransactionByReference;

public record GetTransactionByReferenceQuery : IRequest<TransactionDto?>
{
    public string TransactionReference { get; init; } = null!;
}
