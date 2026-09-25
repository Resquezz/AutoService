using System.Text.Json;
using System.Text.Json.Nodes;
using AutoService.Api.Domain.Events;
using AutoService.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoService.Api.Infrastructure.EventStore;

public class PostgresEventStore : IEventStore
{
    private readonly EventStoreDbContext _context;
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public PostgresEventStore(EventStoreDbContext context)
    {
        _context = context;
    }

    public async Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId)
    {
        var rows = await _context.Events
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.SequenceNumber)
            .ToListAsync();

        var events = new List<IDomainEvent>();
        foreach (var row in rows)
        {
            var envelope = JsonSerializer.Deserialize<DomainEventEnvelope>(row.Payload, SerializerOptions);
            if (envelope is null)
            {
                continue;
            }

            var domainEvent = EventFactory.CreateFromEnvelope(envelope);
            if (domainEvent is not null)
            {
                events.Add(domainEvent);
            }
        }

        return events;
    }

    public async Task AppendAsync(Guid aggregateId, IEnumerable<IDomainEvent> events)
    {
        var eventList = events.ToList();
        if (eventList.Count == 0)
        {
            return;
        }

        var lastSequence = await _context.Events
            .Where(e => e.AggregateId == aggregateId)
            .OrderByDescending(e => e.SequenceNumber)
            .Select(e => e.SequenceNumber)
            .FirstOrDefaultAsync();

        var sequence = lastSequence + 1;
        foreach (var domainEvent in eventList)
        {
            var payloadJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);
            var root = JsonNode.Parse(payloadJson)?.AsObject() ?? new JsonObject();
            root["eventId"] = domainEvent.EventId.ToString();
            root["aggregateId"] = domainEvent.AggregateId.ToString();
            root["aggregateType"] = domainEvent.AggregateType;
            root["eventType"] = domainEvent.EventType;
            root["sequenceNumber"] = sequence;
            root["occurredAt"] = domainEvent.OccurredAt;

            var envelope = new DomainEventEnvelope
            {
                EventId = domainEvent.EventId,
                AggregateId = domainEvent.AggregateId,
                EventType = domainEvent.EventType,
                AggregateType = domainEvent.AggregateType,
                SequenceNumber = sequence,
                OccurredAt = domainEvent.OccurredAt,
                Payload = JsonDocument.Parse(root.ToJsonString()).RootElement
            };

            _context.Events.Add(new EventStoreEntity
            {
                Id = domainEvent.EventId,
                AggregateId = aggregateId,
                AggregateType = domainEvent.AggregateType,
                EventType = domainEvent.EventType,
                SequenceNumber = sequence,
                OccurredAt = domainEvent.OccurredAt,
                Payload = JsonSerializer.Serialize(envelope, SerializerOptions),
                Metadata = JsonSerializer.Serialize(new { domainEvent.EventType, aggregateId }, SerializerOptions)
            });

            sequence++;
        }

        await _context.SaveChangesAsync();
    }
}

public class DomainEventEnvelope
{
    public Guid EventId { get; set; }
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public long SequenceNumber { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public JsonElement Payload { get; set; }
}

public static class EventFactory
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static IDomainEvent? CreateFromEnvelope(DomainEventEnvelope envelope)
    {
        var merged = new JsonObject
        {
            ["eventId"] = envelope.EventId,
            ["aggregateId"] = envelope.AggregateId,
            ["aggregateType"] = envelope.AggregateType,
            ["eventType"] = envelope.EventType,
            ["sequenceNumber"] = envelope.SequenceNumber,
            ["occurredAt"] = envelope.OccurredAt
        };

        foreach (var property in envelope.Payload.EnumerateObject())
        {
            merged[property.Name] = JsonNode.Parse(property.Value.GetRawText());
        }

        var json = merged.ToJsonString();

        return envelope.EventType switch
        {
            nameof(ServiceOrderCreated) => JsonSerializer.Deserialize<ServiceOrderCreated>(json, Options),
            nameof(MechanicAssigned) => JsonSerializer.Deserialize<MechanicAssigned>(json, Options),
            nameof(WorkAdded) => JsonSerializer.Deserialize<WorkAdded>(json, Options),
            nameof(PartReserved) => JsonSerializer.Deserialize<PartReserved>(json, Options),
            nameof(PaymentCompleted) => JsonSerializer.Deserialize<PaymentCompleted>(json, Options),
            nameof(ServiceOrderCompleted) => JsonSerializer.Deserialize<ServiceOrderCompleted>(json, Options),
            _ => null
        };
    }
}
