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
            .Include(o => o.OrderDishes)
                .ThenInclude(od => od.BranchDish)
                    .ThenInclude(bd => bd.Dish)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<(List<Order> items, int totalCount)> GetByUserIdAsync(int userId, int pageNumber, int pageSize)
    {
        var query = _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Branch)
            .Include(o => o.OrderDishes)
                .ThenInclude(od => od.BranchDish)
                    .ThenInclude(bd => bd.Dish)
            .OrderByDescending(o => o.CreatedAt)
            .AsQueryable();

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<Order> items, int totalCount)> GetByBranchIdsAsync(List<int> branchIds, int pageNumber, int pageSize, OrderStatus? status = null)
    {
        var query = _context.Orders
            .Where(o => branchIds.Contains(o.BranchId))
            .Include(o => o.User)
            .Include(o => o.Branch)
            .Include(o => o.OrderDishes)
                .ThenInclude(od => od.BranchDish)
                    .ThenInclude(bd => bd.Dish)
            .OrderByDescending(o => o.CreatedAt)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Order> CreateAsync(Order order, List<OrderDish> orderDishes)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        foreach (var item in orderDishes)
        {
            item.OrderId = order.OrderId;
            item.BranchId = order.BranchId;
        }

        _context.OrderDishes.AddRange(orderDishes);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(order.OrderId))!;
    }

    public async Task<Order> UpdateAsync(Order order, List<OrderDish>? orderDishes = null)
    {
        order.UpdatedAt = DateTime.UtcNow;
        _context.Orders.Update(order);

        if (orderDishes != null)
        {
            var existingItems = await _context.OrderDishes
                .Where(x => x.OrderId == order.OrderId)
                .ToListAsync();

            _context.OrderDishes.RemoveRange(existingItems);

            foreach (var item in orderDishes)
            {
                item.OrderId = order.OrderId;
                item.BranchId = order.BranchId;
            }

            _context.OrderDishes.AddRange(orderDishes);
        }

        await _context.SaveChangesAsync();
        return (await GetByIdAsync(order.OrderId))!;
    }

    public async Task DeleteAsync(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return;
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int orderId)
    {
        return await _context.Orders.AnyAsync(o => o.OrderId == orderId);
    }
}
