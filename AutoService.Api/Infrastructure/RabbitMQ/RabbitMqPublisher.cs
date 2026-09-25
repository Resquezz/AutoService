using System.Text;
using System.Text.Json;
using AutoService.Api.Domain.Events;
using RabbitMQ.Client;

namespace AutoService.Api.Infrastructure.RabbitMQ;

public class RabbitMqPublisher : IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly IConnection? _connection;
    private readonly IChannel? _channel;
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

    public RabbitMqPublisher(IConfiguration configuration)
    {
        _configuration = configuration;

        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = AmqpTcpEndpoint.UseDefaultPort,
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _channel.ExchangeDeclareAsync("autoservice.events", ExchangeType.Topic, durable: true, autoDelete: false).GetAwaiter().GetResult();
    }

    public async Task PublishAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, long startSequence = 1)
    {
        if (_channel is null)
        {
            return;
        }

        var eventList = events.ToList();
        for (var index = 0; index < eventList.Count; index++)
        {
            var domainEvent = eventList[index];
            var sequenceNumber = startSequence + index;
            var payload = new EventEnvelope
            {
                EventId = domainEvent.EventId,
                AggregateId = aggregateId,
                EventType = domainEvent.EventType,
                SequenceNumber = sequenceNumber,
                OccurredAt = domainEvent.OccurredAt,
                Payload = ToPayload(domainEvent)
            };

            var routingKey = RoutingKeyResolver.Resolve(domainEvent);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, SerializerOptions));
            await _channel.BasicPublishAsync(
                exchange: "autoservice.events",
                routingKey: routingKey,
                mandatory: false,
                basicProperties: new BasicProperties { Persistent = true, Type = domainEvent.EventType },
                body: body);
        }
    }

    private static object ToPayload(IDomainEvent @event)
    {
        return @event switch
        {
            ServiceOrderCreated e => new { e.OrderId, e.ClientName, e.VehicleMake, e.VehicleModel, e.Vin, e.LicensePlate },
            MechanicAssigned e => new { e.OrderId, e.MechanicId, e.MechanicName },
            WorkAdded e => new { e.OrderId, e.WorkId, e.WorkName, e.Hours, e.HourlyRate, e.Amount },
            PartReserved e => new { e.OrderId, e.PartId, e.PartName, e.Quantity, e.UnitPrice, e.Amount },
            PaymentCompleted e => new { e.OrderId, e.Amount, e.PaidAt, e.PaymentMethod },
            ServiceOrderCompleted e => new { e.OrderId, e.ClientName, e.VehicleMake, e.VehicleModel, e.CompletedAt, e.TotalAmount },
            _ => new { }
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
        }
    }
}

public sealed class EventEnvelope
{
    public Guid EventId { get; set; }
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public long SequenceNumber { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public object Payload { get; set; } = new();
}

public static class RoutingKeyResolver
{
    public static string Resolve(IDomainEvent @event) => @event switch
    {
        ServiceOrderCreated => "serviceOrder.created",
        MechanicAssigned => "serviceOrder.mechanic_assigned",
        WorkAdded => "serviceOrder.work_added",
        PartReserved => "serviceOrder.part_reserved",
        PaymentCompleted => "serviceOrder.payment_completed",
        ServiceOrderCompleted => "serviceOrder.completed",
        _ => "serviceOrder.unknown"
    };
}
