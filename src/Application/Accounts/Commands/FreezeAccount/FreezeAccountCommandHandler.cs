using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Accounts.Commands.FreezeAccount;

public class FreezeAccountCommandHandler : IRequestHandler<FreezeAccountCommand>
{
    private readonly IAccountRepository _accountRepository;

    public FreezeAccountCommandHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task Handle(FreezeAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account", request.AccountId);

        account.Freeze(request.Reason);

        await _accountRepository.UpdateAsync(account, cancellationToken);
    }
}
