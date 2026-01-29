using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Transactions.DTOs;
using MediatR;

namespace Application.Transactions.Queries.GetAccountTransactions;

public class GetAccountTransactionsQueryHandler : IRequestHandler<GetAccountTransactionsQuery, PaginatedList<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetAccountTransactionsQueryHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<PaginatedList<TransactionDto>> Handle(
        GetAccountTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var transactions = await _transactionRepository.GetByAccountIdAsync(
            request.AccountId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var totalCount = await _transactionRepository.GetCountByAccountIdAsync(
            request.AccountId,
            cancellationToken);

        var dtos = transactions.Select(TransactionDto.FromEntity).ToList();

        return new PaginatedList<TransactionDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
