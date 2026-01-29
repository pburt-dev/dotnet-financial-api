using Application.Accounts.DTOs;
using Domain.Enums;
using MediatR;

namespace Application.Accounts.Commands.CreateAccount;

public record CreateAccountCommand : IRequest<AccountDto>
{
    public string AccountHolderName { get; init; } = null!;
    public AccountType Type { get; init; }
    public string CurrencyCode { get; init; } = "USD";
}
