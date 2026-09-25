using AutoService.Api.Domain.Events;

namespace AutoService.Api.Domain.Aggregates;

public enum ServiceOrderStatus
{
    Created,
    InProgress,
    Paid,
    Completed
}

public class ServiceOrderAggregate
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();

    public Guid Id { get; private set; }
    public Guid? ClientId { get; private set; }
    public string ClientName { get; private set; } = string.Empty;
    public string VehicleMake { get; private set; } = string.Empty;
    public string VehicleModel { get; private set; } = string.Empty;
    public string Vin { get; private set; } = string.Empty;
    public string LicensePlate { get; private set; } = string.Empty;
    public Guid? MechanicId { get; private set; }
    public string MechanicName { get; private set; } = string.Empty;
    public List<ServiceOrderWork> Works { get; private set; } = new();
    public List<ServiceOrderPart> Parts { get; private set; } = new();
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public bool IsPaid { get; private set; }
    public ServiceOrderStatus Status { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public bool IsCompleted => Status == ServiceOrderStatus.Completed;
    public IReadOnlyCollection<IDomainEvent> UncommittedEvents => _uncommittedEvents;

    public static ServiceOrderAggregate Create(Guid orderId, string clientName, string vehicleMake, string vehicleModel, string vin, string licensePlate)
    {
        var aggregate = new ServiceOrderAggregate
        {
            Id = orderId,
            ClientName = clientName,
            VehicleMake = vehicleMake,
            VehicleModel = vehicleModel,
            Vin = vin,
            LicensePlate = licensePlate,
            Status = ServiceOrderStatus.Created
        };

        aggregate._uncommittedEvents.Add(new ServiceOrderCreated
        {
            AggregateId = orderId,
            OrderId = orderId,
            ClientName = clientName,
            VehicleMake = vehicleMake,
            VehicleModel = vehicleModel,
            Vin = vin,
            LicensePlate = licensePlate
        });

        return aggregate;
    }

    public void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ServiceOrderCreated created:
                Id = created.OrderId;
                ClientName = created.ClientName;
                VehicleMake = created.VehicleMake;
                VehicleModel = created.VehicleModel;
                Vin = created.Vin;
                LicensePlate = created.LicensePlate;
                Status = ServiceOrderStatus.Created;
                break;

            case MechanicAssigned mechanicAssigned:
                MechanicId = mechanicAssigned.MechanicId;
                MechanicName = mechanicAssigned.MechanicName;
                Status = ServiceOrderStatus.InProgress;
                break;

            case WorkAdded workAdded:
                Works.Add(new ServiceOrderWork(workAdded.WorkId, workAdded.WorkName, workAdded.Hours, workAdded.HourlyRate, workAdded.Amount));
                TotalAmount += workAdded.Amount;
                break;

            case PartReserved partReserved:
                Parts.Add(new ServiceOrderPart(partReserved.PartId, partReserved.PartName, partReserved.Quantity, partReserved.UnitPrice, partReserved.Amount));
                TotalAmount += partReserved.Amount;
                break;

            case PaymentCompleted paymentCompleted:
                PaidAmount += paymentCompleted.Amount;
                IsPaid = PaidAmount >= TotalAmount;
                Status = IsPaid ? ServiceOrderStatus.Paid : Status;
                break;

            case ServiceOrderCompleted completed:
                CompletedAt = completed.CompletedAt;
                Status = ServiceOrderStatus.Completed;
                break;
        }
    }

    public void LoadFromHistory(IEnumerable<IDomainEvent> events)
    {
        foreach (var @event in events)
        {
            Apply(@event);
        }
    }

    public void AssignMechanic(Guid mechanicId, string mechanicName)
    {
        if (IsCompleted)
            throw new InvalidOperationException("Cannot assign mechanic to a completed order.");

        var evt = new MechanicAssigned
        {
            AggregateId = Id,
            OrderId = Id,
            MechanicId = mechanicId,
            MechanicName = mechanicName
        };
        _uncommittedEvents.Add(evt);
        Apply(evt);
    }

    public void AddWork(string workName, decimal hours, decimal hourlyRate)
    {
        if (string.IsNullOrWhiteSpace(workName)) throw new ArgumentException("Work name is required.");
        if (hours <= 0) throw new ArgumentException("Hours must be greater than zero.");
        if (hourlyRate < 0) throw new ArgumentException("Hourly rate cannot be negative.");
        if (IsCompleted) throw new InvalidOperationException("Cannot add work to completed order.");

        var amount = hours * hourlyRate;
        var evt = new WorkAdded
        {
            AggregateId = Id,
            OrderId = Id,
            WorkId = Guid.NewGuid(),
            WorkName = workName,
            Hours = hours,
            HourlyRate = hourlyRate,
            Amount = amount
        };
        _uncommittedEvents.Add(evt);
        Apply(evt);
    }

    public void ReservePart(string partName, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(partName)) throw new ArgumentException("Part name is required.");
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");
        if (unitPrice < 0) throw new ArgumentException("Unit price cannot be negative.");
        if (IsCompleted) throw new InvalidOperationException("Cannot reserve parts for completed order.");

        var amount = quantity * unitPrice;
        var evt = new PartReserved
        {
            AggregateId = Id,
            OrderId = Id,
            PartId = Guid.NewGuid(),
            PartName = partName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Amount = amount
        };
        _uncommittedEvents.Add(evt);
        Apply(evt);
    }

    public void CompletePayment(decimal amount, string paymentMethod)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be greater than zero.");
        if (IsCompleted) throw new InvalidOperationException("Completed order cannot be paid again.");

        var evt = new PaymentCompleted
        {
            AggregateId = Id,
            OrderId = Id,
            Amount = amount,
            PaidAt = DateTimeOffset.UtcNow,
            PaymentMethod = paymentMethod
        };
        _uncommittedEvents.Add(evt);
        Apply(evt);
    }

    public void Complete()
    {
        if (IsCompleted) throw new InvalidOperationException("Order is already completed.");
        if (!IsPaid) throw new InvalidOperationException("Cannot complete order before payment is made.");

        var evt = new ServiceOrderCompleted
        {
            AggregateId = Id,
            OrderId = Id,
            ClientName = ClientName,
            VehicleMake = VehicleMake,
            VehicleModel = VehicleModel,
            CompletedAt = DateTimeOffset.UtcNow,
            TotalAmount = TotalAmount
        };
        _uncommittedEvents.Add(evt);
        Apply(evt);
    }

    public void MarkEventsApplied()
    {
        _uncommittedEvents.Clear();
    }
}

public sealed record ServiceOrderWork(Guid Id, string Name, decimal Hours, decimal HourlyRate, decimal Amount);

public sealed record ServiceOrderPart(Guid Id, string Name, int Quantity, decimal UnitPrice, decimal Amount);
