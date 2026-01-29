using Application.Accounts.DTOs;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Accounts.Queries.GetAccountBalance;

public class GetAccountBalanceQueryHandler : IRequestHandler<GetAccountBalanceQuery, AccountBalanceDto?>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IDateTimeService _dateTimeService;

    public GetAccountBalanceQueryHandler(
        IAccountRepository accountRepository,
        IDateTimeService dateTimeService)
    {
        _accountRepository = accountRepository;
        _dateTimeService = dateTimeService;
    }

    public async Task<AccountBalanceDto?> Handle(GetAccountBalanceQuery request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account == null)
            return null;

        return new AccountBalanceDto
        {
            AccountId = account.Id,
            AccountNumber = account.AccountNumber,
            Balance = account.Balance.Amount,
            CurrencyCode = account.Balance.CurrencyCode,
            AsOf = _dateTimeService.UtcNow
        };
    }
}
