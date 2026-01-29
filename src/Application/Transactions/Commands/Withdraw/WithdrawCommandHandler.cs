using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Transactions.DTOs;
using Domain.ValueObjects;
using MediatR;

namespace Application.Transactions.Commands.Withdraw;

public class WithdrawCommandHandler : IRequestHandler<WithdrawCommand, TransactionDto>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;

    public WithdrawCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<TransactionDto> Handle(WithdrawCommand request, CancellationToken cancellationToken)
    {
        // Check for existing transaction with same idempotency key
        var existingTransaction = await _transactionRepository.GetByIdempotencyKeyAsync(
            request.AccountId,
            request.IdempotencyKey,
            cancellationToken);

        if (existingTransaction != null)
        {
            return TransactionDto.FromEntity(existingTransaction);
        }

        // Load account without transactions to avoid EF Core InMemory tracking issues
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account", request.AccountId);

        var money = new Money(request.Amount, request.CurrencyCode);
        var transaction = account.Withdraw(money, request.IdempotencyKey, request.Description);

        // Save the transaction explicitly (account is tracked, balance will be saved too)
        await _transactionRepository.AddAsync(transaction, cancellationToken);

        return TransactionDto.FromEntity(transaction);
    }
}
