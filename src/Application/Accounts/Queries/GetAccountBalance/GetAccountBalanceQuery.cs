using Application.Accounts.DTOs;
using MediatR;

namespace Application.Accounts.Queries.GetAccountBalance;

public record GetAccountBalanceQuery : IRequest<AccountBalanceDto?>
{
    public Guid AccountId { get; init; }
}
