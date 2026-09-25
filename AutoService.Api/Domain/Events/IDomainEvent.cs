namespace AutoService.Api.Domain.Events;

public interface IDomainEvent
{
    Guid EventId { get; }
    Guid AggregateId { get; }
    string AggregateType { get; }
    string EventType { get; }
    long SequenceNumber { get; }
    DateTimeOffset OccurredAt { get; }
}

public abstract record DomainEventBase : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid AggregateId { get; init; }
    public string AggregateType { get; init; } = "ServiceOrder";
    public string EventType => GetType().Name;
    public long SequenceNumber { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
