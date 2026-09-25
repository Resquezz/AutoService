using AutoService.Api.Domain.Aggregates;
using AutoService.Api.Domain.Events;

namespace AutoService.Tests;

public class ServiceOrderAggregateTests
{
    [Fact]
    public void Create_ShouldGenerateCreatedEvent()
    {
        var orderId = Guid.NewGuid();
        var aggregate = ServiceOrderAggregate.Create(orderId, "Іван Петренко", "Toyota", "Camry", "VIN123", "BC1234AB");

        Assert.NotEmpty(aggregate.UncommittedEvents);
        Assert.Contains(aggregate.UncommittedEvents, e => e is ServiceOrderCreated);
    }

    [Fact]
    public void Replay_ShouldRestoreState()
    {
        var result = CreateAggregateWithPayment();
        var history = result.UncommittedEvents.ToList();

        var replayed = new ServiceOrderAggregate();
        replayed.LoadFromHistory(history);

        Assert.Equal("Іван Петренко", replayed.ClientName);
        Assert.Equal("Toyota", replayed.VehicleMake);
        Assert.Equal("Camry", replayed.VehicleModel);
        Assert.Equal("BC1234AB", replayed.LicensePlate);
        Assert.Equal(ServiceOrderStatus.Paid, replayed.Status);
    }

    [Fact]
    public void AddWork_ShouldIncreaseTotal()
    {
        var aggregate = ServiceOrderAggregate.Create(Guid.NewGuid(), "Client", "Toyota", "Corolla", "VIN1", "AA1234BB");
        aggregate.AddWork("Заміна мастила", 2, 600m);

        Assert.Equal(1200m, aggregate.UncommittedEvents.OfType<WorkAdded>().First().Amount);
        Assert.Equal(1200m, aggregate.TotalAmount);
    }

    [Fact]
    public void Payment_ShouldSetPaidState()
    {
        var aggregate = CreateAggregateWithPayment();
        Assert.True(aggregate.IsPaid);
        Assert.Equal(ServiceOrderStatus.Paid, aggregate.Status);
    }

    [Fact]
    public void Complete_ShouldThrow_WhenNotPaid()
    {
        var aggregate = ServiceOrderAggregate.Create(Guid.NewGuid(), "Client", "Toyota", "Corolla", "VIN2", "AA4321CC");
        aggregate.AddWork("Ремонт", 1m, 500m);

        var ex = Assert.Throws<InvalidOperationException>(() => aggregate.Complete());
        Assert.Contains("payment", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Complete_AfterPayment_ShouldGenerateCompletedEvent()
    {
        var aggregate = CreateAggregateWithPayment();
        aggregate.Complete();

        Assert.Contains(aggregate.UncommittedEvents, e => e is ServiceOrderCompleted);
        Assert.Equal(ServiceOrderStatus.Completed, aggregate.Status);
    }

    private static ServiceOrderAggregate CreateAggregateWithPayment()
    {
        var aggregate = ServiceOrderAggregate.Create(Guid.NewGuid(), "Іван Петренко", "Toyota", "Camry", "VIN123", "BC1234AB");
        aggregate.AddWork("Заміна мастила", 2m, 600m);
        aggregate.ReservePart("Масляний фільтр", 1, 450m);
        aggregate.CompletePayment(1650m, "Card");
        return aggregate;
    }
}
