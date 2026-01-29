using MediatR;

namespace Application.Accounts.Commands.FreezeAccount;

public record FreezeAccountCommand : IRequest
{
    public Guid AccountId { get; init; }
    public string Reason { get; init; } = null!;
}
