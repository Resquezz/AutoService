namespace AutoService.Api.Application.Commands;

public record CreateServiceOrderCommand(
    string ClientName,
    string VehicleMake,
    string VehicleModel,
    string Vin,
    string LicensePlate);

public record AssignMechanicCommand(
    Guid MechanicId,
    string MechanicName);

public record AddWorkCommand(
    string WorkName,
    decimal Hours,
    decimal HourlyRate);

public record ReservePartCommand(
    string PartName,
    int Quantity,
    decimal UnitPrice);

public record PaymentCommand(
    decimal Amount,
    string PaymentMethod);

public record CommandResult(
    Guid OrderId,
    Guid EventId,
    string Message);
