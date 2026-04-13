using BO.DTO.Cart;

namespace Service.Interfaces;

public interface ICartService
{
    Task<List<CartResponseDto>> GetMyCartsAsync(int userId);
    Task<CartResponseDto> GetMyCartByBranchAsync(int userId, int branchId);
    Task<CartResponseDto> AddItemAsync(int userId, AddCartItemRequest request);
    Task<CartResponseDto> UpdateItemQuantityAsync(int userId, int branchId, int dishId, UpdateCartItemRequest request);
    Task<CartResponseDto> RemoveItemAsync(int userId, int branchId, int dishId);
    Task<CartResponseDto> ClearCartAsync(int userId, int branchId);
    Task<CheckoutCartResponseDto> CheckoutAsync(int userId, CheckoutCartRequest request);
}
