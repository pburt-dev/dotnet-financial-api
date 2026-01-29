using MediatR;

namespace Application.Accounts.Commands.UnfreezeAccount;

public record UnfreezeAccountCommand : IRequest
{
    public Guid AccountId { get; init; }
}
