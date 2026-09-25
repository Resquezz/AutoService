using AutoService.Api.Application.Commands;
using AutoService.Api.Domain.Aggregates;
using AutoService.Api.Domain.Events;
using AutoService.Api.Infrastructure.EventStore;
using AutoService.Api.Infrastructure.RabbitMQ;

namespace AutoService.Api.Application.Services;

public class OrderCommandService
{
    private readonly IEventStore _eventStore;
    private readonly RabbitMqPublisher _publisher;

    public OrderCommandService(IEventStore eventStore, RabbitMqPublisher publisher)
    {
        _eventStore = eventStore;
        _publisher = publisher;
    }

    public async Task<CommandResult> CreateAsync(CreateServiceOrderCommand command)
    {
        var orderId = Guid.NewGuid();
        var aggregate = ServiceOrderAggregate.Create(orderId, command.ClientName, command.VehicleMake, command.VehicleModel, command.Vin, command.LicensePlate);
        var events = aggregate.UncommittedEvents.ToList();

        await _eventStore.AppendAsync(orderId, events);
        await _publisher.PublishAsync(orderId, events, 1);
        var firstEvent = events.First();

        aggregate.MarkEventsApplied();
        return new CommandResult(orderId, firstEvent.EventId, "Service order created");
    }

    public async Task<CommandResult> AssignMechanicAsync(Guid orderId, AssignMechanicCommand command)
    {
        var aggregate = await LoadAsync(orderId);
        aggregate.AssignMechanic(command.MechanicId, command.MechanicName);
        await SaveAndPublishAsync(orderId, aggregate);
        var evt = aggregate.UncommittedEvents.First();
        return new CommandResult(orderId, evt.EventId, "Mechanic assigned");
    }

    public async Task<CommandResult> AddWorkAsync(Guid orderId, AddWorkCommand command)
    {
        var aggregate = await LoadAsync(orderId);
        aggregate.AddWork(command.WorkName, command.Hours, command.HourlyRate);
        await SaveAndPublishAsync(orderId, aggregate);
        var evt = aggregate.UncommittedEvents.First();
        return new CommandResult(orderId, evt.EventId, "Work added");
    }

    public async Task<CommandResult> ReservePartAsync(Guid orderId, ReservePartCommand command)
    {
        var aggregate = await LoadAsync(orderId);
        aggregate.ReservePart(command.PartName, command.Quantity, command.UnitPrice);
        await SaveAndPublishAsync(orderId, aggregate);
        var evt = aggregate.UncommittedEvents.First();
        return new CommandResult(orderId, evt.EventId, "Part reserved");
    }

    public async Task<CommandResult> PayAsync(Guid orderId, PaymentCommand command)
    {
        var aggregate = await LoadAsync(orderId);
        aggregate.CompletePayment(command.Amount, command.PaymentMethod);
        await SaveAndPublishAsync(orderId, aggregate);
        var evt = aggregate.UncommittedEvents.First();
        return new CommandResult(orderId, evt.EventId, "Payment completed");
    }

    public async Task<CommandResult> CompleteAsync(Guid orderId)
    {
        var aggregate = await LoadAsync(orderId);
        aggregate.Complete();
        await SaveAndPublishAsync(orderId, aggregate);
        var evt = aggregate.UncommittedEvents.First();
        return new CommandResult(orderId, evt.EventId, "Service order completed");
    }

    public async Task<List<IDomainEvent>> GetEventsAsync(Guid orderId)
    {
        return await _eventStore.GetEventsAsync(orderId);
    }

    private async Task<ServiceOrderAggregate> LoadAsync(Guid orderId)
    {
        var history = await _eventStore.GetEventsAsync(orderId);
        if (history.Count == 0)
        {
            throw new KeyNotFoundException($"Order {orderId} not found.");
        }

        var aggregate = new ServiceOrderAggregate();
        aggregate.LoadFromHistory(history);
        return aggregate;
    }

    private async Task SaveAndPublishAsync(Guid orderId, ServiceOrderAggregate aggregate)
    {
        var events = aggregate.UncommittedEvents.ToList();
        if (events.Count == 0)
        {
            return;
        }

        var currentSequence = (await _eventStore.GetEventsAsync(orderId)).Count + 1;
        await _eventStore.AppendAsync(orderId, events);
        await _publisher.PublishAsync(orderId, events, currentSequence);
        aggregate.MarkEventsApplied();
    }
}
