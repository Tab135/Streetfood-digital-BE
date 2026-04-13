using BO.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL;

public class CartDAO
{
    private readonly StreetFoodDbContext _context;

    public CartDAO(StreetFoodDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Cart?> GetByUserIdAsync(int userId)
    {
        return await _context.Carts
            .Include(c => c.Branch)
            .Include(c => c.Items)
                .ThenInclude(i => i.Dish)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Cart>> GetByUserIdAllAsync(int userId)
    {
        return await _context.Carts
            .Include(c => c.Branch)
            .Include(c => c.Items)
                .ThenInclude(i => i.Dish)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Cart?> GetByUserAndBranchAsync(int userId, int branchId)
    {
        return await _context.Carts
            .Include(c => c.Branch)
            .Include(c => c.Items)
                .ThenInclude(i => i.Dish)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.BranchId == branchId);
    }

    public async Task<Cart> CreateAsync(Cart cart)
    {
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        return cart;
    }

    public async Task UpdateAsync(Cart cart)
    {
        cart.UpdatedAt = DateTime.UtcNow;
        _context.Carts.Update(cart);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int cartId)
    {
        var cart = await _context.Carts.FindAsync(cartId);
        if (cart == null)
        {
            return;
        }

        _context.Carts.Remove(cart);
        await _context.SaveChangesAsync();
    }

    public async Task<CartItem?> GetItemByDishIdAsync(int cartId, int dishId)
    {
        return await _context.CartItems.FirstOrDefaultAsync(i => i.CartId == cartId && i.DishId == dishId);
    }

    public async Task<CartItem> AddItemAsync(CartItem item)
    {
        _context.CartItems.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task UpdateItemAsync(CartItem item)
    {
        item.UpdatedAt = DateTime.UtcNow;
        _context.CartItems.Update(item);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(CartItem item)
    {
        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();
    }

    public async Task ClearItemsAsync(int cartId)
    {
        var items = await _context.CartItems.Where(i => i.CartId == cartId).ToListAsync();
        if (items.Count == 0)
        {
            return;
        }

        _context.CartItems.RemoveRange(items);
        await _context.SaveChangesAsync();
    }
}
