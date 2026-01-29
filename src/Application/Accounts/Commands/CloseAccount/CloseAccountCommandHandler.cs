using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Accounts.Commands.CloseAccount;

public class CloseAccountCommandHandler : IRequestHandler<CloseAccountCommand>
{
    private readonly IAccountRepository _accountRepository;

    public CloseAccountCommandHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task Handle(CloseAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account", request.AccountId);

        account.Close();

        await _accountRepository.UpdateAsync(account, cancellationToken);
    }
}
