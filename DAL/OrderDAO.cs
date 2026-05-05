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
            .Include(o => o.AppliedVoucher)
            .Include(o => o.OrderDishes)
                .ThenInclude(od => od.BranchDish)
                    .ThenInclude(bd => bd.Dish)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<(List<Order> items, int totalCount)> GetByUserIdAsync(int userId, int pageNumber, int pageSize, List<OrderStatus>? statuses = null)
    {
        var query = _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Branch)
            .Include(o => o.AppliedVoucher)
            .Include(o => o.OrderDishes)
                .ThenInclude(od => od.BranchDish)
                    .ThenInclude(bd => bd.Dish)
            .OrderByDescending(o => o.CreatedAt)
            .AsQueryable();

        if (statuses != null && statuses.Count > 0)
        {
            query = query.Where(o => statuses.Contains(o.Status));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Order?> GetLatestPendingByUserAndBranchAsync(int userId, int branchId)
    {
        return await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Branch)
            .Include(o => o.OrderDishes)
                .ThenInclude(od => od.BranchDish)
                    .ThenInclude(bd => bd.Dish)
            .Where(o => o.UserId == userId
                     && o.BranchId == branchId
                     && o.Status == OrderStatus.Pending)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Order>> GetPendingOrdersNotUpdatedSinceAsync(DateTime staleBeforeUtc)
    {
        return await _context.Orders
            .Where(o => o.Status == OrderStatus.Pending && o.UpdatedAt <= staleBeforeUtc)
            .OrderBy(o => o.UpdatedAt)
            .ToListAsync();
    }

    public async Task<(List<Order> items, int totalCount)> GetByBranchIdsAsync(List<int> branchIds, int pageNumber, int pageSize, List<OrderStatus>? statuses = null)
    {
        var query = _context.Orders
            .Where(o => branchIds.Contains(o.BranchId))
            .Include(o => o.User)
            .Include(o => o.Branch)
            .Include(o => o.AppliedVoucher)
            .Include(o => o.OrderDishes)
                .ThenInclude(od => od.BranchDish)
                    .ThenInclude(bd => bd.Dish)
            .OrderByDescending(o => o.CreatedAt)
            .AsQueryable();

        if (statuses != null && statuses.Count > 0)
        {
            query = query.Where(o => statuses.Contains(o.Status));
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

    public async Task<HashSet<int>> GetOrderIdsWithPaymentsAsync(IEnumerable<int> orderIds)
    {
        var idList = orderIds.ToList();
        var result = await _context.Payments
            .Where(p => p.OrderId.HasValue && idList.Contains(p.OrderId.Value))
            .Select(p => p.OrderId!.Value)
            .Distinct()
            .ToListAsync();
        return [.. result];
    }

    public async Task<(List<Order> items, Dictionary<int, Payment> paymentByOrderId, int totalCount)> GetAllForAdminAsync(
        int pageNumber,
        int pageSize,
        List<OrderStatus>? statuses = null,
        int? branchId = null,
        int? userId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.Branch).ThenInclude(b => b.Vendor)
            .Include(o => o.AppliedVoucher)
            .Include(o => o.OrderDishes)
                .ThenInclude(od => od.BranchDish)
                    .ThenInclude(bd => bd.Dish)
            .AsQueryable();

        if (statuses != null && statuses.Count > 0)
            query = query.Where(o => statuses.Contains(o.Status));

        if (branchId.HasValue)
            query = query.Where(o => o.BranchId == branchId.Value);

        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);

        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate.Value);

        query = query.OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync();
        var orders = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var orderIds = orders.Select(o => o.OrderId).ToList();
        var payments = await _context.Payments
            .Where(p => p.OrderId.HasValue && orderIds.Contains(p.OrderId.Value))
            .ToListAsync();

        var paymentByOrderId = payments
            .GroupBy(p => p.OrderId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.CreatedAt).First());

        return (orders, paymentByOrderId, totalCount);
    }
}
