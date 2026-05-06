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
        private const string VendorOrderCommissionPercentSettingName = "VendorOrderCommissionPercent";
        private const int DefaultVendorOrderCommissionPercent = 10;

        private readonly StreetFoodDbContext _context;

        public VendorDashboardDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<(int? vendorId, System.Collections.Generic.List<int>? allowedBranchIds)> GetDashboardContextByUserIdAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (null, null);

            if (user.Role == BO.Entities.Role.Vendor)
            {
                var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.UserId == userId);
                return (vendor?.VendorId, null);
            }
            else if (user.Role == BO.Entities.Role.Manager)
            {
                var branches = await _context.Branches.Where(b => b.ManagerId == userId).ToListAsync();
                var vendorId = branches.FirstOrDefault()?.VendorId;
                var branchIds = branches.Select(b => b.BranchId).ToList();
                return (vendorId, branchIds);
            }
            
            return (null, null);
        }

        public async Task<RevenueDashboardDto> GetRevenueDashboardAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate)
        {
            var startDate = fromDate.Date;
            var endDate = toDate.Date;
            var endExclusive = endDate.AddDays(1);
            var (previousStartDate, previousEndExclusive) = GetPreviousPeriod(startDate, endDate);

            var branchIds = allowedBranchIds ?? await _context.Branches
                .Where(b => b.VendorId == vendorId)
                .Select(b => b.BranchId)
                .ToListAsync();

            if (!branchIds.Any())
            {
                return new RevenueDashboardDto();
            }

            var completedOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => branchIds.Contains(o.BranchId)
                            && o.Status == OrderStatus.Complete
                            && o.CreatedAt >= startDate
                            && o.CreatedAt < endExclusive)
                .Select(o => new
                {
                    o.CreatedAt,
                    o.TotalAmount,
                    o.FinalAmount,
                    o.CommissionRate,
                    IsSystemVoucher = o.AppliedVoucherId.HasValue
                        && (o.AppliedVoucher!.VendorCampaignId == null
                            || (o.AppliedVoucher.VendorCampaign != null
                                && !o.AppliedVoucher.VendorCampaign.CreatedByVendorId.HasValue))
                })
                .ToListAsync();

            decimal totalRevenue = completedOrders.Sum(o => CalculateVendorNetRevenue(o.TotalAmount, o.FinalAmount, o.IsSystemVoucher, o.CommissionRate ?? 0m));
            int totalOrders = completedOrders.Count;

            var previousCompletedOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => branchIds.Contains(o.BranchId)
                            && o.Status == OrderStatus.Complete
                            && o.CreatedAt >= previousStartDate
                            && o.CreatedAt < previousEndExclusive)
                .Select(o => new
                {
                    o.TotalAmount,
                    o.FinalAmount,
                    o.CommissionRate,
                    IsSystemVoucher = o.AppliedVoucherId.HasValue
                        && (o.AppliedVoucher!.VendorCampaignId == null
                            || (o.AppliedVoucher.VendorCampaign != null
                                && !o.AppliedVoucher.VendorCampaign.CreatedByVendorId.HasValue))
                })
                .ToListAsync();

            var previousTotalRevenue = previousCompletedOrders.Sum(o => CalculateVendorNetRevenue(o.TotalAmount, o.FinalAmount, o.IsSystemVoucher, o.CommissionRate ?? 0m));

            var dailyRevenues = completedOrders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new DailyRevenueDto
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => CalculateVendorNetRevenue(o.TotalAmount, o.FinalAmount, o.IsSystemVoucher, o.CommissionRate ?? 0m)),
                    OrderCount = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            return new RevenueDashboardDto
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                RevenueGrowthRate = CalculateGrowthRate(totalRevenue, previousTotalRevenue),
                PreviousPeriod = $"từ {previousStartDate:dd-MM-yyyy} tới {previousEndExclusive.AddDays(-1):dd-MM-yyyy}",
                DailyRevenues = dailyRevenues
            };

        }

        public async Task<RevenueBarChartDto> GetRevenueBarChartAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate)
        {
            var startDate = fromDate.Date;
            var endDate = toDate.Date;
            var endExclusive = endDate.AddDays(1);
            var (previousStartDate, previousEndExclusive) = GetPreviousPeriod(startDate, endDate);

            var branchIds = allowedBranchIds ?? await _context.Branches
                .Where(b => b.VendorId == vendorId)
                .Select(b => b.BranchId)
                .ToListAsync();

            var result = new RevenueBarChartDto();

            if (!branchIds.Any())
            {
                // return two zero-value bars with correct ranges
                result.Items.Add(new BarChartItemDto
                {
                    Label = "Previous",
                    FromDate = previousStartDate,
                    ToDate = previousEndExclusive.AddDays(-1),
                    Value = 0m
                });
                result.Items.Add(new BarChartItemDto
                {
                    Label = "Now",
                    FromDate = startDate,
                    ToDate = endExclusive.AddDays(-1),
                    Value = 0m
                });

                return result;
            }

            var currentOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => branchIds.Contains(o.BranchId)
                            && o.Status == OrderStatus.Complete
                            && o.CreatedAt >= startDate
                            && o.CreatedAt < endExclusive)
                .Select(o => new
                {
                    o.TotalAmount,
                    o.FinalAmount,
                    o.CommissionRate,
                    IsSystemVoucher = o.AppliedVoucherId.HasValue
                        && (o.AppliedVoucher!.VendorCampaignId == null
                            || (o.AppliedVoucher.VendorCampaign != null
                                && !o.AppliedVoucher.VendorCampaign.CreatedByVendorId.HasValue))
                })
                .ToListAsync();

            var currentTotal = currentOrders.Sum(o => CalculateVendorNetRevenue(o.TotalAmount, o.FinalAmount, o.IsSystemVoucher, o.CommissionRate ?? 0m));

            var previousOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => branchIds.Contains(o.BranchId)
                            && o.Status == OrderStatus.Complete
                            && o.CreatedAt >= previousStartDate
                            && o.CreatedAt < previousEndExclusive)
                .Select(o => new
                {
                    o.TotalAmount,
                    o.FinalAmount,
                    o.CommissionRate,
                    IsSystemVoucher = o.AppliedVoucherId.HasValue
                        && (o.AppliedVoucher!.VendorCampaignId == null
                            || (o.AppliedVoucher.VendorCampaign != null
                                && !o.AppliedVoucher.VendorCampaign.CreatedByVendorId.HasValue))
                })
                .ToListAsync();

            var previousTotal = previousOrders.Sum(o => CalculateVendorNetRevenue(o.TotalAmount, o.FinalAmount, o.IsSystemVoucher, o.CommissionRate ?? 0m));


            result.Items.Add(new BarChartItemDto
            {
                Label = "Previous",
                FromDate = previousStartDate,
                ToDate = previousEndExclusive.AddDays(-1),
                Value = previousTotal
            });

            result.Items.Add(new BarChartItemDto
            {
                Label = "Now",
                FromDate = startDate,
                ToDate = endExclusive.AddDays(-1),
                Value = currentTotal
            });

            return result;
        }

        public async Task<CampaignDashboardDto> GetCampaignDashboardAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate)
        {
            var vendorCampaigns = await _context.Campaigns
                .AsNoTracking()
                .Where(c => c.CreatedByVendorId == vendorId)
                .ToListAsync();

            if (!vendorCampaigns.Any())
            {
                return new CampaignDashboardDto();
            }

            var campaignPerformances = new List<CampaignPerformanceDto>();

            foreach (var campaign in vendorCampaigns)
            {
                var joinedBranches = await _context.BranchCampaigns
                    .AsNoTracking()
                    .Where(bc => bc.CampaignId == campaign.CampaignId)
                    .Select(bc => new { bc.BranchId, bc.Branch.Name })
                    .ToListAsync();

                var orders = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.Status == OrderStatus.Complete
                                && o.AppliedVoucher != null
                                && o.AppliedVoucher.VendorCampaignId == campaign.CampaignId
                                && o.CreatedAt >= fromDate
                                && o.CreatedAt <= toDate)
                    .Select(o => new
                    {
                        o.BranchId,
                        o.TotalAmount,
                        o.FinalAmount,
                        o.CommissionRate,
                        IsSystemVoucher = o.AppliedVoucherId.HasValue
                            && (o.AppliedVoucher!.VendorCampaignId == null
                                || (o.AppliedVoucher.VendorCampaign != null
                                    && !o.AppliedVoucher.VendorCampaign.CreatedByVendorId.HasValue))
                    })
                    .ToListAsync();

                var branchesDto = new List<VendorCampaignBranchDto>();
                foreach (var b in joinedBranches)
                {
                    var branchOrders = orders.Where(o => o.BranchId == b.BranchId).ToList();
                    branchesDto.Add(new VendorCampaignBranchDto
                    {
                        BranchId = b.BranchId,
                        BranchName = b.Name,
                        OrderCount = branchOrders.Count,
                        Revenue = branchOrders.Sum(o => CalculateVendorNetRevenue(o.TotalAmount, o.FinalAmount, o.IsSystemVoucher, o.CommissionRate ?? 0m))
                    });
                }

                campaignPerformances.Add(new CampaignPerformanceDto
                {
                    CampaignId = campaign.CampaignId,
                    CampaignName = campaign.Name,
                    OrderCount = orders.Count,
                    Revenue = orders.Sum(o => CalculateVendorNetRevenue(o.TotalAmount, o.FinalAmount, o.IsSystemVoucher, o.CommissionRate ?? 0m)),
                    Branches = branchesDto
                });
            }

            return new CampaignDashboardDto
            {
                TotalCampaigns = campaignPerformances.Count,
                TotalCampaignOrders = campaignPerformances.Sum(c => c.OrderCount),
                TotalCampaignRevenue = campaignPerformances.Sum(c => c.Revenue),
                Campaigns = campaignPerformances
            };
        }

        public async Task<VoucherDashboardDto> GetVoucherDashboardAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate)
        {
            var startDate = fromDate.Date;
            var endDate = toDate.Date;
            var endExclusive = endDate.AddDays(1);

            var branchIds = allowedBranchIds ?? await _context.Branches
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
                            && o.CreatedAt >= startDate
                            && o.CreatedAt < endExclusive
                            && o.AppliedVoucher!.VendorCampaign != null 
                            && o.AppliedVoucher.VendorCampaign.CreatedByVendorId == vendorId)
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

        public async Task<DishDashboardDto> GetDishDashboardAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate)
        {
            var hasDateFilter = !(fromDate == DateTime.MinValue && toDate == DateTime.MaxValue);
            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MaxValue;
            DateTime endExclusive = DateTime.MaxValue;
            if (hasDateFilter)
            {
                startDate = fromDate.Date;
                endDate = toDate.Date;
                endExclusive = endDate == DateTime.MaxValue.Date ? DateTime.MaxValue : endDate.AddDays(1);
            }

            var allDishes = await _context.Dishes
                .Where(d => d.VendorId == vendorId)
                .ToListAsync();

            var branchIds = allowedBranchIds ?? await _context.Branches
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
                var orderDishesQuery = _context.OrderDishes
                    .Where(od => od.BranchId.HasValue && branchIds.Contains(od.BranchId.Value)
                                && od.Order.Status == OrderStatus.Complete);

                if (hasDateFilter)
                {
                    orderDishesQuery = orderDishesQuery.Where(od => od.Order.CreatedAt >= startDate && od.Order.CreatedAt < endExclusive);
                }

                var topDishesQuery = await orderDishesQuery
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

        public async Task<BranchesPerformanceDashboardDto> GetBranchesPerformanceAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate)
        {
            var startDate = fromDate.Date;
            var endDate = toDate.Date;
            var endExclusive = endDate.AddDays(1);

            var branchQuery = _context.Branches
                .AsNoTracking()
                .Where(b => b.VendorId == vendorId);

            // Filter by allowed branches if specified
            if (allowedBranchIds != null && allowedBranchIds.Any())
            {
                branchQuery = branchQuery.Where(b => allowedBranchIds.Contains(b.BranchId));
            }

            var branches = await branchQuery
                .Select(b => new { b.BranchId, b.Name })
                .ToListAsync();

            if (!branches.Any())
            {
                return new BranchesPerformanceDashboardDto();
            }

            var branchIds = branches.Select(b => b.BranchId).ToList();
            var completedOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => branchIds.Contains(o.BranchId)
                            && o.Status == OrderStatus.Complete
                            && o.CreatedAt >= startDate
                            && o.CreatedAt < endExclusive)
                .Select(o => new
                {
                    o.BranchId,
                    o.TotalAmount,
                    o.FinalAmount,
                    o.CommissionRate,
                    IsSystemVoucher = o.AppliedVoucherId.HasValue
                        && (o.AppliedVoucher!.VendorCampaignId == null
                            || (o.AppliedVoucher.VendorCampaign != null
                                && !o.AppliedVoucher.VendorCampaign.CreatedByVendorId.HasValue))
                })
                .ToListAsync();

            var branchPerformances = completedOrders
                .GroupBy(o => o.BranchId)
                .Select(g => new
                {
                    BranchId = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => CalculateVendorNetRevenue(o.TotalAmount, o.FinalAmount, o.IsSystemVoucher, o.CommissionRate ?? 0m))
                })
                .ToList();


            var result = new List<BranchPerformanceDto>();
            foreach (var branch in branches)
            {
                var performance = branchPerformances.FirstOrDefault(bp => bp.BranchId == branch.BranchId);
                result.Add(new BranchPerformanceDto
                {
                    BranchId = branch.BranchId,
                    BranchName = branch.Name,
                    OrderCount = performance?.OrderCount ?? 0,
                    Revenue = performance?.Revenue ?? 0m
                });
            }

            return new BranchesPerformanceDashboardDto
            {
                Branches = result
            };
        }

        private static (DateTime PreviousStartDate, DateTime PreviousEndExclusive) GetPreviousPeriod(DateTime startDate, DateTime endDate)
        {
            var dayCount = (endDate - startDate).Days + 1;
            var previousEndExclusive = startDate;
            var previousStartDate = startDate.AddDays(-dayCount);

            return (previousStartDate, previousEndExclusive);
        }

        private static decimal CalculateGrowthRate(decimal currentValue, decimal previousValue)
        {
            if (previousValue == 0m)
            {
                return currentValue == 0m ? 0m : 100m;
            }

            return Math.Round(((currentValue - previousValue) / previousValue) * 100m, 2);
        }

        public async Task<decimal> GetCommissionRateAsync()
        {
            var rawPercent = await _context.Settings
                .AsNoTracking()
                .Where(s => s.Name == VendorOrderCommissionPercentSettingName)
                .Select(s => s.Value)
                .FirstOrDefaultAsync();

            var percent = int.TryParse(rawPercent, out var parsedPercent)
                ? parsedPercent
                : DefaultVendorOrderCommissionPercent;

            return Math.Clamp(percent, 0, 100);
        }

        private async Task<decimal> GetVendorOrderCommissionRateAsync()
        {
            var rawPercent = await _context.Settings
                .AsNoTracking()
                .Where(s => s.Name == VendorOrderCommissionPercentSettingName)
                .Select(s => s.Value)
                .FirstOrDefaultAsync();

            var percent = int.TryParse(rawPercent, out var parsedPercent)
                ? parsedPercent
                : DefaultVendorOrderCommissionPercent;

            percent = Math.Clamp(percent, 0, 100);
            return percent / 100m;
        }

        private static decimal CalculateVendorNetRevenue(decimal totalAmount, decimal finalAmount, bool isSystemVoucher, decimal commissionRate)
        {
            var grossReceivable = isSystemVoucher ? totalAmount : finalAmount;
            var commissionBase = isSystemVoucher ? finalAmount : totalAmount;
            var commissionAmount = Math.Round(commissionBase * commissionRate, 2, MidpointRounding.AwayFromZero);
            var netRevenue = grossReceivable - commissionAmount;

            return netRevenue < 0m ? 0m : netRevenue;
        }
    }
}






