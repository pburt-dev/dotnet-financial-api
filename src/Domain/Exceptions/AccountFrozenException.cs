namespace Domain.Exceptions;

public class AccountFrozenException : DomainException
{
    public Guid AccountId { get; }
    public string? Reason { get; }

    public AccountFrozenException(Guid accountId, string? reason = null)
        : base($"Account {accountId} is frozen." + (reason != null ? $" Reason: {reason}" : ""))
    {
        AccountId = accountId;
        Reason = reason;
    }
}
