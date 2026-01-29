using Application.Common.Interfaces;
using Application.Transactions.DTOs;
using MediatR;

namespace Application.Transactions.Queries.GetTransactionByReference;

public class GetTransactionByReferenceQueryHandler : IRequestHandler<GetTransactionByReferenceQuery, TransactionDto?>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionByReferenceQueryHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<TransactionDto?> Handle(GetTransactionByReferenceQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByReferenceAsync(request.TransactionReference, cancellationToken);

        return transaction != null ? TransactionDto.FromEntity(transaction) : null;
    }
}
