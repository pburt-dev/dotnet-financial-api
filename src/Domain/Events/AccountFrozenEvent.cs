namespace Domain.Events;

public sealed class AccountFrozenEvent : DomainEvent
{
    public Guid AccountId { get; }
    public string Reason { get; }

    public AccountFrozenEvent(Guid accountId, string reason)
    {
        AccountId = accountId;
        Reason = reason;
    }
}
