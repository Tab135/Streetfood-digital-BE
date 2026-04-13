using BO.Entities;

namespace Repository.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(int userId);
    Task<List<Cart>> GetByUserIdAllAsync(int userId);
    Task<Cart?> GetByUserAndBranchAsync(int userId, int branchId);
    Task<Cart> CreateAsync(Cart cart);
    Task UpdateAsync(Cart cart);
    Task DeleteAsync(int cartId);
    Task<CartItem?> GetItemByDishIdAsync(int cartId, int dishId);
    Task<CartItem> AddItemAsync(CartItem item);
    Task UpdateItemAsync(CartItem item);
    Task RemoveItemAsync(CartItem item);
    Task ClearItemsAsync(int cartId);
}
