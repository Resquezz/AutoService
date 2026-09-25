using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoService.Consumer.Models;

[Table("ServiceOrderReadModels")]
public class ServiceOrderReadModel
{
    [Key]
    public Guid Id { get; set; }

    public string ClientName { get; set; } = string.Empty;
    public string VehicleMake { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string Vin { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public Guid? MechanicId { get; set; }
    public string MechanicName { get; set; } = string.Empty;
    public string Status { get; set; } = "Created";
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table("ServiceOrderWorkReadModels")]
public class ServiceOrderWorkReadModel
{
    [Key]
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string WorkName { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal Amount { get; set; }
}

[Table("ServiceOrderPartReadModels")]
public class ServiceOrderPartReadModel
{
    [Key]
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

[Table("ProcessedEvents")]
public class ProcessedEvent
{
    [Key]
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
