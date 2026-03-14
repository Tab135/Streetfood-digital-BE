using BO.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL;

public class OrderDAO
{
    private readonly StreetFoodDbContext _context;

    public OrderDAO(StreetFoodDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Order?> GetByIdAsync(int orderId)
    {
        return await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Branch)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<bool> ExistsAsync(int orderId)
    {
        return await _context.Orders.AnyAsync(o => o.OrderId == orderId);
    }
}
