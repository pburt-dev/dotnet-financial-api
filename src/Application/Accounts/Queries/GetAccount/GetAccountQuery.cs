using Application.Accounts.DTOs;
using MediatR;

namespace Application.Accounts.Queries.GetAccount;

public record GetAccountQuery : IRequest<AccountDto?>
{
    public Guid AccountId { get; init; }
}
