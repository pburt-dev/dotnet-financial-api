using Application.Common.Models;
using Application.Transactions.DTOs;
using MediatR;

namespace Application.Transactions.Queries.GetAccountTransactions;

public record GetAccountTransactionsQuery : IRequest<PaginatedList<TransactionDto>>
{
    public Guid AccountId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
