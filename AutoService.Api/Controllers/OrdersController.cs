using AutoService.Api.Application.Commands;
using AutoService.Api.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoService.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderCommandService _service;

    public OrdersController(OrderCommandService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<CommandResult>> Create([FromBody] CreateServiceOrderCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ClientName)) return BadRequest("ClientName is required.");
        if (string.IsNullOrWhiteSpace(command.Vin)) return BadRequest("VIN is required.");
        if (string.IsNullOrWhiteSpace(command.LicensePlate)) return BadRequest("LicensePlate is required.");

        var result = await _service.CreateAsync(command);
        return Ok(result);
    }

    [HttpPost("{id:guid}/mechanic")]
    public async Task<ActionResult<CommandResult>> AssignMechanic(Guid id, [FromBody] AssignMechanicCommand command)
    {
        try
        {
            var result = await _service.AssignMechanicAsync(id, command);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("{id:guid}/works")]
    public async Task<ActionResult<CommandResult>> AddWork(Guid id, [FromBody] AddWorkCommand command)
    {
        try
        {
            var result = await _service.AddWorkAsync(id, command);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("{id:guid}/parts")]
    public async Task<ActionResult<CommandResult>> ReservePart(Guid id, [FromBody] ReservePartCommand command)
    {
        try
        {
            var result = await _service.ReservePartAsync(id, command);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("{id:guid}/payment")]
    public async Task<ActionResult<CommandResult>> Payment(Guid id, [FromBody] PaymentCommand command)
    {
        try
        {
            var result = await _service.PayAsync(id, command);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<CommandResult>> Complete(Guid id)
    {
        try
        {
            var result = await _service.CompleteAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpGet("{id:guid}/events")]
    public async Task<ActionResult<List<object>>> GetEvents(Guid id)
    {
        var events = await _service.GetEventsAsync(id);
        var result = events.Select(x => new
        {
            SequenceNumber = x.SequenceNumber,
            EventType = x.EventType,
            OccurredAt = x.OccurredAt
        }).ToList();

        return Ok(result);
    }
}
