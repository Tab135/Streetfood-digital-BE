using BO.DTO.Dashboard;
using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class VendorDashboardDAO
    {
        private readonly StreetFoodDbContext _context;

        public VendorDashboardDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<int?> GetVendorIdByUserIdAsync(int userId)
        {
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.UserId == userId);
            return vendor?.VendorId;
        }

        public async Task<RevenueDashboardDto> GetRevenueDashboardAsync(int vendorId, DateTime fromDate, DateTime toDate)
        {
            var branchIds = await _context.Branches
                .Where(b => b.VendorId == vendorId)
                .Select(b => b.BranchId)
                .ToListAsync();

            if (!branchIds.Any())
            {
                return new RevenueDashboardDto();
            }

            // Keep this query projected/aggregated to avoid selecting unused columns.
            var completedOrdersQuery = _context.Orders
                .AsNoTracking()
                .Where(o => branchIds.Contains(o.BranchId)
                            && o.Status == OrderStatus.Complete
                            && o.CreatedAt >= fromDate
                            && o.CreatedAt <= toDate);

            decimal totalRevenue = await completedOrdersQuery.SumAsync(o => (decimal?)o.FinalAmount) ?? 0m;
            int totalOrders = await completedOrdersQuery.CountAsync();

            var dailyRevenues = await completedOrdersQuery
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new DailyRevenueDto
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.FinalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToListAsync();

            return new RevenueDashboardDto
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                DailyRevenues = dailyRevenues
            };
        }

        public async Task<VoucherDashboardDto> GetVoucherDashboardAsync(int vendorId)
        {
            var branchIds = await _context.Branches
                .Where(b => b.VendorId == vendorId)
                .Select(b => b.BranchId)
                .ToListAsync();

            if (!branchIds.Any())
            {
                return new VoucherDashboardDto();
            }

            var voucherUsages = await _context.Orders
                .Where(o => branchIds.Contains(o.BranchId) 
                            && o.Status == OrderStatus.Complete 
                            && o.AppliedVoucherId != null
                            && o.AppliedVoucher!.Campaign != null 
                            && o.AppliedVoucher.Campaign.CreatedByVendorId == vendorId)
                .Include(o => o.AppliedVoucher)
                .GroupBy(o => new { o.AppliedVoucher!.Type, o.AppliedVoucher.Name })
                .Select(g => new VoucherUsageDto
                {
                    VoucherType = g.Key.Type,
                    VoucherName = g.Key.Name,
                    UsageCount = g.Count()
                })
                .ToListAsync();

            return new VoucherDashboardDto
            {
                VoucherUsages = voucherUsages
            };
        }

        public async Task<DishDashboardDto> GetDishDashboardAsync(int vendorId)
        {
            var allDishes = await _context.Dishes
                .Where(d => d.VendorId == vendorId)
                .ToListAsync();

            var branchIds = await _context.Branches
                .Where(b => b.VendorId == vendorId)
                .Select(b => b.BranchId)
                .ToListAsync();

            if (!allDishes.Any())
            {
                return new DishDashboardDto();
            }
            
            var quantityDict = new System.Collections.Generic.Dictionary<int, int>();

            if (branchIds.Any())
            {
                var topDishesQuery = await _context.OrderDishes
                    .Where(od => od.BranchId.HasValue && branchIds.Contains(od.BranchId.Value) && od.Order.Status == OrderStatus.Complete)
                    .GroupBy(od => new { od.DishId, od.DishName })
                    .Select(g => new
                    {
                        DishId = g.Key.DishId,
                        DishName = g.Key.DishName,
                        TotalQuantityOrdered = g.Sum(od => od.Quantity)
                    })
                    .ToListAsync();
                    
                var activeOrKnownDishes = topDishesQuery
                    .Where(q => q.DishId.HasValue)
                    .ToDictionary(
                        q => q.DishId!.Value, 
                        q => new { q.DishName, q.TotalQuantityOrdered }
                    );

                // Add active/inactive dishes that belong to vendor but haven't been ordered
                var topDishes = new List<TopDishDto>();
                foreach (var d in allDishes)
                {
                    topDishes.Add(new TopDishDto
                    {
                        DishId = d.DishId,
                        DishName = d.Name,
                        TotalQuantityOrdered = activeOrKnownDishes.ContainsKey(d.DishId) ? activeOrKnownDishes[d.DishId].TotalQuantityOrdered : 0
                    });
                }
                
                // Add historical dishes that were completely deleted from Dishes table but still exist in OrderDishes
                foreach (var historical in topDishesQuery.Where(q => !q.DishId.HasValue || !allDishes.Any(d => d.DishId == q.DishId.Value)))
                {
                    topDishes.Add(new TopDishDto
                    {
                        DishId = historical.DishId, // it could be null or point to a non-existent dish
                        DishName = !string.IsNullOrEmpty(historical.DishName) ? historical.DishName : "Món ăn đã xoá",
                        TotalQuantityOrdered = historical.TotalQuantityOrdered
                    });
                }

                return new DishDashboardDto
                {
                    TopDishes = topDishes
                        .OrderByDescending(d => d.TotalQuantityOrdered)
                        .ThenBy(d => d.DishName)
                        .Take(10) // Usually dashboards show top 10
                        .ToList()
                };
            }

            // Fallback if no branches
            var fallbackTopDishes = allDishes.Select(d => new TopDishDto
                {
                    DishId = d.DishId,
                    DishName = d.Name,
                    TotalQuantityOrdered = 0
                })
                .OrderByDescending(d => d.TotalQuantityOrdered)
                .ThenBy(d => d.DishName)
                .ToList();

            return new DishDashboardDto
            {
                TopDishes = fallbackTopDishes
            };
        }
    }
}
