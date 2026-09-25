using AutoService.Api.Domain.Events;

namespace AutoService.Api.Infrastructure.EventStore;

public interface IEventStore
{
    Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId);
    Task AppendAsync(Guid aggregateId, IEnumerable<IDomainEvent> events);
}
