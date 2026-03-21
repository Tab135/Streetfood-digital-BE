using BO.Common;
using BO.DTO.Cart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.Security.Claims;

namespace StreetFood.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "User")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiResponse<CartResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyCart()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var cart = await _cartService.GetMyCartAsync(userId);
        return Ok(new
        {
            message = "Get cart successfully",
            data = cart
        });
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(ApiResponse<CartResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var cart = await _cartService.AddItemAsync(userId, request);
        return Ok(new
        {
            message = "Added item to cart successfully",
            data = cart
        });
    }

    [HttpPut("items/{dishId}")]
    [ProducesResponseType(typeof(ApiResponse<CartResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateItemQuantity(int dishId, [FromBody] UpdateCartItemRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var cart = await _cartService.UpdateItemQuantityAsync(userId, dishId, request);
        return Ok(new
        {
            message = "Cart item updated successfully",
            data = cart
        });
    }

    [HttpDelete("items/{dishId}")]
    [ProducesResponseType(typeof(ApiResponse<CartResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveItem(int dishId)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var cart = await _cartService.RemoveItemAsync(userId, dishId);
        return Ok(new
        {
            message = "Cart item removed successfully",
            data = cart
        });
    }

    [HttpDelete("clear")]
    [ProducesResponseType(typeof(ApiResponse<CartResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearCart()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var cart = await _cartService.ClearCartAsync(userId);
        return Ok(new
        {
            message = "Cart cleared successfully",
            data = cart
        });
    }

    [HttpPost("checkout")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutCartResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Checkout([FromBody] CheckoutCartRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var result = await _cartService.CheckoutAsync(userId, request);
        return Ok(new
        {
            message = "Checkout successful",
            data = result
        });
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out userId);
    }
}
