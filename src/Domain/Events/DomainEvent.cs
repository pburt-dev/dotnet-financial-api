using MediatR;

namespace Domain.Events;

/// <summary>
/// Base class for all domain events.
/// </summary>
public abstract class DomainEvent : INotification
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
