using System.Text.Json;
using AutoService.Api.Infrastructure.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AutoService.AuditConsumer.Consumer;

public class AuditWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;

    public AuditWorker(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("[AUDIT] Connecting to RabbitMQ...");
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
            UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await _channel.ExchangeDeclareAsync("autoservice.events", ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync("autoservice.audit.completed", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync("autoservice.audit.completed", "autoservice.events", "*.completed", cancellationToken: stoppingToken);
        Console.WriteLine("[AUDIT] Listening for completed orders...");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = System.Text.Encoding.UTF8.GetString(body);
            var envelope = JsonSerializer.Deserialize<EventEnvelope>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (envelope is null)
            {
                await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: stoppingToken);
                return;
            }

            if (envelope.EventType == "ServiceOrderCompleted")
            {
                var payload = envelope.Payload as JsonElement? ?? JsonDocument.Parse(JsonSerializer.Serialize(envelope.Payload)).RootElement;
                var completedAt = payload.TryGetProperty("completedAt", out var completedProp) ? completedProp.GetDateTimeOffset() : DateTimeOffset.UtcNow;
                var total = payload.TryGetProperty("totalAmount", out var totalProp) ? totalProp.GetDecimal() : 0m;
                var clientName = payload.TryGetProperty("clientName", out var clientProp) ? clientProp.GetString() ?? "Unknown" : "Unknown";
                var vehicle = payload.TryGetProperty("vehicleMake", out var vehicleMake) && payload.TryGetProperty("vehicleModel", out var vehicleModel)
                    ? $"{vehicleMake.GetString()} {vehicleModel.GetString()}"
                    : "Unknown";

                Console.WriteLine("[AUDIT]");
                Console.WriteLine("Completed order received");
                Console.WriteLine($"OrderId: {envelope.AggregateId}");
                Console.WriteLine($"Vehicle: {vehicle}");
                Console.WriteLine($"Client: {clientName}");
                Console.WriteLine($"Total: {total}");
                Console.WriteLine($"CompletedAt: {completedAt:O}");
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: stoppingToken);
        };

        await _channel.BasicConsumeAsync("autoservice.audit.completed", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }
}
