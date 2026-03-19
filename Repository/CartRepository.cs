using BO.Entities;
using DAL;
using Repository.Interfaces;

namespace Repository;

public class CartRepository : ICartRepository
{
    private readonly CartDAO _cartDao;

    public CartRepository(CartDAO cartDao)
    {
        _cartDao = cartDao ?? throw new ArgumentNullException(nameof(cartDao));
    }

    public Task<Cart?> GetByUserIdAsync(int userId) => _cartDao.GetByUserIdAsync(userId);

    public Task<Cart> CreateAsync(Cart cart) => _cartDao.CreateAsync(cart);

    public Task UpdateAsync(Cart cart) => _cartDao.UpdateAsync(cart);

    public Task<CartItem?> GetItemByDishIdAsync(int cartId, int dishId) => _cartDao.GetItemByDishIdAsync(cartId, dishId);

    public Task<CartItem> AddItemAsync(CartItem item) => _cartDao.AddItemAsync(item);

    public Task UpdateItemAsync(CartItem item) => _cartDao.UpdateItemAsync(item);

    public Task RemoveItemAsync(CartItem item) => _cartDao.RemoveItemAsync(item);

    public Task ClearItemsAsync(int cartId) => _cartDao.ClearItemsAsync(cartId);
}
