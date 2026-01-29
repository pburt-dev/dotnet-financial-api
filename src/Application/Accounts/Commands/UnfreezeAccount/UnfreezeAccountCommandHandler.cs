using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Accounts.Commands.UnfreezeAccount;

public class UnfreezeAccountCommandHandler : IRequestHandler<UnfreezeAccountCommand>
{
    private readonly IAccountRepository _accountRepository;

    public UnfreezeAccountCommandHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task Handle(UnfreezeAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account", request.AccountId);

        account.Unfreeze();

        await _accountRepository.UpdateAsync(account, cancellationToken);
    }
}
