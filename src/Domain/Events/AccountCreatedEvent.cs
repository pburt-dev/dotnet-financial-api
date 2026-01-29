namespace Domain.Events;

public sealed class AccountCreatedEvent : DomainEvent
{
    public Guid AccountId { get; }
    public string AccountNumber { get; }
    public string AccountHolderName { get; }

    public AccountCreatedEvent(Guid accountId, string accountNumber, string accountHolderName)
    {
        AccountId = accountId;
        AccountNumber = accountNumber;
        AccountHolderName = accountHolderName;
    }
}
