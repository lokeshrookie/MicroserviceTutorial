using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using OrderService.Services;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]  // All endpoints require a valid JWT (defense-in-depth — Gateway also enforces this)
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IProductServiceClient _productClient;

    public OrdersController(OrderDbContext db, IProductServiceClient productClient)
    {
        _db            = db;
        _productClient = productClient;
    }

    // ─── GET /api/orders ──────────────────────────────────────────────────────

    /// <summary>Returns all orders.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,User")]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        var orders = await _db.Orders.ToListAsync();
        return Ok(orders);
    }

    // ─── GET /api/orders/{id} ─────────────────────────────────────────────────

    /// <summary>Returns a single order by ID.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null)
            return NotFound(new { Message = $"Order with ID {id} was not found." });

        return Ok(order);
    }

    // ─── POST /api/orders ─────────────────────────────────────────────────────

    /// <summary>
    /// Places a new order.
    ///
    /// INTER-SERVICE CALL: Before persisting, OrderService contacts ProductService
    /// directly (on the internal Docker network) to validate that the given ProductId
    /// exists. The caller's JWT is forwarded so ProductService can authenticate the
    /// internal request.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        // Forward the caller's Authorization header to ProductService
        var authHeader = Request.Headers["Authorization"].ToString();

        var productExists = await _productClient.ProductExistsAsync(request.ProductId, authHeader);

        if (!productExists)
        {
            return BadRequest(new
            {
                Message = $"Product with ID {request.ProductId} does not exist. " +
                           "Please provide a valid ProductId."
            });
        }

        var order = new Order
        {
            ProductId = request.ProductId,
            Quantity  = request.Quantity,
            OrderDate = DateTime.UtcNow,
            Status    = "Placed"
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
    }

    // ─── DELETE /api/orders/{id} ──────────────────────────────────────────────

    /// <summary>
    /// Cancels an order (soft-delete). Sets Status = "Cancelled".
    /// Admin role required.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null)
            return NotFound(new { Message = $"Order with ID {id} was not found." });

        if (order.Status == "Cancelled")
            return Conflict(new { Message = $"Order {id} is already cancelled." });

        order.Status = "Cancelled";
        await _db.SaveChangesAsync();

        return Ok(new { Message = $"Order {id} has been cancelled.", Order = order });
    }
}

// ─── Request DTOs ─────────────────────────────────────────────────────────────

/// <summary>Payload for placing an order.</summary>
public record CreateOrderRequest(
    [Required][Range(1, int.MaxValue, ErrorMessage = "ProductId must be a positive integer.")] int ProductId,
    [Required][Range(1, 1000,         ErrorMessage = "Quantity must be between 1 and 1000.")]  int Quantity
);
