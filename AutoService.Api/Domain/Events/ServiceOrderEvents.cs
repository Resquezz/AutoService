namespace AutoService.Api.Domain.Events;

public sealed record ServiceOrderCreated : DomainEventBase
{
    public Guid OrderId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string VehicleMake { get; init; } = string.Empty;
    public string VehicleModel { get; init; } = string.Empty;
    public string Vin { get; init; } = string.Empty;
    public string LicensePlate { get; init; } = string.Empty;
}

public sealed record MechanicAssigned : DomainEventBase
{
    public Guid OrderId { get; init; }
    public Guid MechanicId { get; init; }
    public string MechanicName { get; init; } = string.Empty;
}

public sealed record WorkAdded : DomainEventBase
{
    public Guid OrderId { get; init; }
    public Guid WorkId { get; init; } = Guid.NewGuid();
    public string WorkName { get; init; } = string.Empty;
    public decimal Hours { get; init; }
    public decimal HourlyRate { get; init; }
    public decimal Amount { get; init; }
}

public sealed record PartReserved : DomainEventBase
{
    public Guid OrderId { get; init; }
    public Guid PartId { get; init; } = Guid.NewGuid();
    public string PartName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Amount { get; init; }
}

public sealed record PaymentCompleted : DomainEventBase
{
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public DateTimeOffset PaidAt { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
}

public sealed record ServiceOrderCompleted : DomainEventBase
{
    public Guid OrderId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string VehicleMake { get; init; } = string.Empty;
    public string VehicleModel { get; init; } = string.Empty;
    public DateTimeOffset CompletedAt { get; init; }
    public decimal TotalAmount { get; init; }
}
