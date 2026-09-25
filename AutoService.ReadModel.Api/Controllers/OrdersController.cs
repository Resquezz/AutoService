using AutoService.ReadModel.Api.Models;
using AutoService.ReadModel.Api.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoService.ReadModel.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly ReadModelDbContext _db;

    public OrdersController(ReadModelDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ServiceOrderReadModel>>> GetOrders()
    {
        var orders = await _db.ServiceOrders.OrderBy(x => x.UpdatedAt).ToListAsync();
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceOrderReadModel>> GetOrder(Guid id)
    {
        var order = await _db.ServiceOrders.FirstOrDefaultAsync(x => x.Id == id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet("{id:guid}/works")]
    public async Task<ActionResult<List<ServiceOrderWorkReadModel>>> GetWorks(Guid id)
    {
        var works = await _db.ServiceOrderWorks.Where(x => x.OrderId == id).ToListAsync();
        return Ok(works);
    }

    [HttpGet("{id:guid}/parts")]
    public async Task<ActionResult<List<ServiceOrderPartReadModel>>> GetParts(Guid id)
    {
        var parts = await _db.ServiceOrderParts.Where(x => x.OrderId == id).ToListAsync();
        return Ok(parts);
    }
}
