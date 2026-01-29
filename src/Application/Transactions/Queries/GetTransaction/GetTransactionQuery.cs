using Application.Transactions.DTOs;
using MediatR;

namespace Application.Transactions.Queries.GetTransaction;

public record GetTransactionQuery : IRequest<TransactionDto?>
{
    public Guid TransactionId { get; init; }
}
