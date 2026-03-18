using BO.DTO.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.Security.Claims;

namespace StreetFood.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
    }

    [HttpPost]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var created = await _orderService.CreateOrderAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = created.OrderId }, new
        {
            message = "Order created successfully",
            data = created
        });
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var order = await _orderService.GetOrderByIdAsync(id, userId);
        if (order == null)
        {
            return NotFound(new { message = "Order not found" });
        }

        return Ok(new
        {
            message = "Get order successfully",
            data = order
        });
    }

    [HttpGet("my-orders")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> GetMyOrders([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var orders = await _orderService.GetMyOrdersAsync(userId, pageNumber, pageSize);
        return Ok(new
        {
            message = "Get orders successfully",
            data = orders
        });
    }

    [HttpGet("vendor/orders")]
    [Authorize(Roles = "Vendor")]
    public async Task<IActionResult> GetVendorOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] BO.Entities.OrderStatus? status = null)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var orders = await _orderService.GetVendorOrdersAsync(userId, pageNumber, pageSize, status);
        return Ok(new
        {
            message = "Get vendor orders successfully",
            data = orders
        });
    }

    [HttpPut("vendor/orders/{id}/decision")]
    [Authorize(Roles = "Vendor")]
    public async Task<IActionResult> VendorDecision(int id, [FromQuery] bool approve)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var updated = await _orderService.VendorDecideOrderAsync(id, userId, approve);
        return Ok(new
        {
            message = approve ? "Order confirmed successfully" : "Order rejected successfully",
            data = updated
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var updated = await _orderService.UpdateOrderAsync(id, request, userId);
        return Ok(new
        {
            message = "Order updated successfully",
            data = updated
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        await _orderService.DeleteOrderAsync(id, userId);
        return Ok(new { message = "Order deleted successfully" });
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out userId);
    }
}
