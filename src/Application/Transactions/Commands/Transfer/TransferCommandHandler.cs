using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Transactions.DTOs;
using Domain.ValueObjects;
using MediatR;

namespace Application.Transactions.Commands.Transfer;

public class TransferCommandHandler : IRequestHandler<TransferCommand, TransferResultDto>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;

    public TransferCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<TransferResultDto> Handle(TransferCommand request, CancellationToken cancellationToken)
    {
        // Check for existing transaction with same idempotency key
        var existingTransaction = await _transactionRepository.GetByIdempotencyKeyAsync(
            request.SourceAccountId,
            request.IdempotencyKey,
            cancellationToken);

        if (existingTransaction != null)
        {
            var existingDestTransaction = await _transactionRepository.GetByIdempotencyKeyAsync(
                request.DestinationAccountId,
                $"{request.IdempotencyKey}-in",
                cancellationToken);

            return new TransferResultDto
            {
                SourceTransaction = TransactionDto.FromEntity(existingTransaction),
                DestinationTransaction = existingDestTransaction != null
                    ? TransactionDto.FromEntity(existingDestTransaction)
                    : null!
            };
        }

        // Load accounts with transactions included for proper EF Core tracking
        var sourceAccount = await _accountRepository.GetByIdWithTransactionsAsync(request.SourceAccountId, cancellationToken)
            ?? throw new NotFoundException("Source Account", request.SourceAccountId);

        var destinationAccount = await _accountRepository.GetByIdWithTransactionsAsync(request.DestinationAccountId, cancellationToken)
            ?? throw new NotFoundException("Destination Account", request.DestinationAccountId);

        var money = new Money(request.Amount, request.CurrencyCode);

        // Perform the transfer - transactions are added to accounts' collections
        var sourceTransaction = sourceAccount.TransferOut(money, request.DestinationAccountId, request.IdempotencyKey);
        var destinationTransaction = destinationAccount.TransferIn(money, request.SourceAccountId, request.IdempotencyKey);

        // Save all changes - transactions cascade from accounts
        await _transactionRepository.SaveAsync(cancellationToken);

        return new TransferResultDto
        {
            SourceTransaction = TransactionDto.FromEntity(sourceTransaction),
            DestinationTransaction = TransactionDto.FromEntity(destinationTransaction)
        };
    }
}
