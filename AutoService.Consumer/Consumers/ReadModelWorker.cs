using System.Text.Json;
using AutoService.Api.Infrastructure.RabbitMQ;
using AutoService.Consumer.Persistence;
using AutoService.Consumer.Projection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AutoService.Consumer.Consumers;

public class ReadModelWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;

    public ReadModelWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("[Consumer] Connecting to RabbitMQ...");
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
        await _channel.QueueDeclareAsync("autoservice.readmodel", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync("autoservice.readmodel", "autoservice.events", "serviceOrder.#", cancellationToken: stoppingToken);
        Console.WriteLine("[Consumer] Connected");
        Console.WriteLine("[Consumer] Waiting for messages...");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = System.Text.Encoding.UTF8.GetString(body);
                var envelope = JsonSerializer.Deserialize<EventEnvelope>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (envelope is null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    return;
                }

                Console.WriteLine($"[Consumer] Event received: {envelope.EventType}");
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ReadModelDbContext>();
                var projection = scope.ServiceProvider.GetRequiredService<ServiceOrderProjection>();
                await projection.ApplyAsync(db, envelope, stoppingToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                Console.WriteLine($"[Projection] Read model updated for {envelope.AggregateId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Consumer] Error processing message: {ex.Message}");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(queue: "autoservice.readmodel", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
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
