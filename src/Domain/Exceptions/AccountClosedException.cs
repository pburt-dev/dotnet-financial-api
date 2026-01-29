namespace Domain.Exceptions;

public class AccountClosedException : DomainException
{
    public Guid AccountId { get; }

    public AccountClosedException(Guid accountId)
        : base($"Account {accountId} is closed and cannot be used for transactions.")
    {
        AccountId = accountId;
    }
}
