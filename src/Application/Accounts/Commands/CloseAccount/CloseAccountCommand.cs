using MediatR;

namespace Application.Accounts.Commands.CloseAccount;

public record CloseAccountCommand : IRequest
{
    public Guid AccountId { get; init; }
}
