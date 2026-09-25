using System.Text.Json;
using AutoService.Api.Infrastructure.RabbitMQ;
using AutoService.Consumer.Models;
using AutoService.Consumer.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoService.Consumer.Projection;

public class ServiceOrderProjection
{
    public async Task ApplyAsync(ReadModelDbContext db, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        if (await db.ProcessedEvents.AnyAsync(x => x.EventId == envelope.EventId, cancellationToken))
        {
            return;
        }

        var payload = envelope.Payload as JsonElement? ?? JsonDocument.Parse(JsonSerializer.Serialize(envelope.Payload)).RootElement;

        switch (envelope.EventType)
        {
            case "ServiceOrderCreated":
                var createdModel = new ServiceOrderReadModel
                {
                    Id = envelope.AggregateId,
                    ClientName = payload.TryGetProperty("clientName", out var clientProp) ? clientProp.GetString() ?? string.Empty : string.Empty,
                    VehicleMake = payload.TryGetProperty("vehicleMake", out var makeProp) ? makeProp.GetString() ?? string.Empty : string.Empty,
                    VehicleModel = payload.TryGetProperty("vehicleModel", out var modelProp) ? modelProp.GetString() ?? string.Empty : string.Empty,
                    Vin = payload.TryGetProperty("vin", out var vinProp) ? vinProp.GetString() ?? string.Empty : string.Empty,
                    LicensePlate = payload.TryGetProperty("licensePlate", out var plateProp) ? plateProp.GetString() ?? string.Empty : string.Empty,
                    Status = "Created",
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                db.ServiceOrders.Add(createdModel);
                break;

            case "MechanicAssigned":
                var assignedOrder = await db.ServiceOrders.FirstOrDefaultAsync(x => x.Id == envelope.AggregateId, cancellationToken);
                if (assignedOrder is null) break;
                assignedOrder.MechanicName = payload.TryGetProperty("mechanicName", out var mechanicName) ? mechanicName.GetString() ?? string.Empty : string.Empty;
                assignedOrder.Status = "InProgress";
                assignedOrder.UpdatedAt = DateTimeOffset.UtcNow;
                break;

            case "WorkAdded":
                var workOrder = await db.ServiceOrders.FirstOrDefaultAsync(x => x.Id == envelope.AggregateId, cancellationToken) ?? new ServiceOrderReadModel { Id = envelope.AggregateId, Status = "Created" };
                var workId = payload.TryGetProperty("workId", out var workIdProp) ? Guid.Parse(workIdProp.GetString() ?? Guid.Empty.ToString()) : Guid.NewGuid();
                db.ServiceOrderWorks.Add(new ServiceOrderWorkReadModel
                {
                    Id = workId,
                    OrderId = envelope.AggregateId,
                    WorkName = payload.TryGetProperty("workName", out var workNameProp) ? workNameProp.GetString() ?? string.Empty : string.Empty,
                    Hours = payload.TryGetProperty("hours", out var hoursProp) ? hoursProp.GetDecimal() : 0m,
                    HourlyRate = payload.TryGetProperty("hourlyRate", out var rateProp) ? rateProp.GetDecimal() : 0m,
                    Amount = payload.TryGetProperty("amount", out var workAmountProp) ? workAmountProp.GetDecimal() : 0m
                });
                if (workOrder.Id == envelope.AggregateId)
                {
                    workOrder.TotalAmount += payload.TryGetProperty("amount", out var totalProp) ? totalProp.GetDecimal() : 0m;
                    workOrder.UpdatedAt = DateTimeOffset.UtcNow;
                }
                break;

            case "PartReserved":
                var partOrder = await db.ServiceOrders.FirstOrDefaultAsync(x => x.Id == envelope.AggregateId, cancellationToken) ?? new ServiceOrderReadModel { Id = envelope.AggregateId, Status = "Created" };
                var partId = payload.TryGetProperty("partId", out var partIdProp) ? Guid.Parse(partIdProp.GetString() ?? Guid.Empty.ToString()) : Guid.NewGuid();
                db.ServiceOrderParts.Add(new ServiceOrderPartReadModel
                {
                    Id = partId,
                    OrderId = envelope.AggregateId,
                    PartName = payload.TryGetProperty("partName", out var partNameProp) ? partNameProp.GetString() ?? string.Empty : string.Empty,
                    Quantity = payload.TryGetProperty("quantity", out var qtyProp) ? qtyProp.GetInt32() : 0,
                    UnitPrice = payload.TryGetProperty("unitPrice", out var unitPriceProp) ? unitPriceProp.GetDecimal() : 0m,
                    Amount = payload.TryGetProperty("amount", out var partAmountProp) ? partAmountProp.GetDecimal() : 0m
                });
                if (partOrder.Id == envelope.AggregateId)
                {
                    partOrder.TotalAmount += payload.TryGetProperty("amount", out var totalProp) ? totalProp.GetDecimal() : 0m;
                    partOrder.UpdatedAt = DateTimeOffset.UtcNow;
                }
                break;

            case "PaymentCompleted":
                var paymentOrder = await db.ServiceOrders.FirstOrDefaultAsync(x => x.Id == envelope.AggregateId, cancellationToken);
                if (paymentOrder is null) break;
                paymentOrder.PaidAmount += payload.TryGetProperty("amount", out var amountPayProp) ? amountPayProp.GetDecimal() : 0m;
                paymentOrder.IsPaid = true;
                paymentOrder.Status = "Paid";
                paymentOrder.UpdatedAt = DateTimeOffset.UtcNow;
                break;

            case "ServiceOrderCompleted":
                var completedOrder = await db.ServiceOrders.FirstOrDefaultAsync(x => x.Id == envelope.AggregateId, cancellationToken);
                if (completedOrder is null) break;
                completedOrder.Status = "Completed";
                completedOrder.CompletedAt = payload.TryGetProperty("completedAt", out var completedAt) ? completedAt.GetDateTimeOffset() : DateTimeOffset.UtcNow;
                completedOrder.UpdatedAt = DateTimeOffset.UtcNow;
                Console.WriteLine($"[Projection] Order completed: {envelope.AggregateId}");
                break;
        }

        db.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = envelope.EventId,
            EventType = envelope.EventType,
            AggregateId = envelope.AggregateId,
            ProcessedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
