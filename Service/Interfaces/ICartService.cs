using BO.DTO.Cart;

namespace Service.Interfaces;

public interface ICartService
{
    Task<CartResponseDto> GetMyCartAsync(int userId);
    Task<CartResponseDto> AddItemAsync(int userId, AddCartItemRequest request);
    Task<CartResponseDto> UpdateItemQuantityAsync(int userId, int dishId, UpdateCartItemRequest request);
    Task<CartResponseDto> RemoveItemAsync(int userId, int dishId);
    Task<CartResponseDto> ClearCartAsync(int userId);
    Task<CheckoutCartResponseDto> CheckoutAsync(int userId, CheckoutCartRequest request);
}
