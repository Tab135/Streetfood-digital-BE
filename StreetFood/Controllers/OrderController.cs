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

    [HttpGet("{id}/pickup-code")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> GetPickupCode(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var pickupCode = await _orderService.GetOrderPickupCodeAsync(id, userId);
        return Ok(new
        {
            message = "Get pickup code successfully",
            data = pickupCode
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

    [HttpGet("vendor/branches/{branchId}/orders")]
    [Authorize(Roles = "Vendor,Manager")]
    public async Task<IActionResult> GetVendorOrdersByBranch(
        int branchId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] BO.Entities.OrderStatus? status = null)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var orders = await _orderService.GetVendorOrdersByBranchAsync(userId, branchId, pageNumber, pageSize, status);
        return Ok(new
        {
            message = "Get vendor branch orders successfully",
            data = orders
        });
    }

    [HttpGet("manager/orders")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> GetManagerOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] BO.Entities.OrderStatus? status = null)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var orders = await _orderService.GetManagerOrdersAsync(userId, pageNumber, pageSize, status);
        return Ok(new
        {
            message = "Get manager orders successfully",
            data = orders
        });
    }

    [HttpPut("vendor/orders/{id}/decision")]
    [Authorize(Roles = "Vendor,Manager")]
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

    [HttpPut("vendor/orders/{id}/complete")]
    [Authorize(Roles = "Vendor,Manager")]
    public async Task<IActionResult> VendorComplete(int id, [FromQuery] string verificationCode)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var completedOptions = await _orderService.VendorCompleteOrderAsync(id, userId, verificationCode);
        return Ok(new
        {
            message = "Order completed successfully after verification, funds have been transferred to vendor",
            data = completedOptions
        });
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out userId);
    }
}
