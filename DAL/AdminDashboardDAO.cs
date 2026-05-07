using BO.DTO.Dashboard;
using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class AdminDashboardDAO
    {
        private readonly StreetFoodDbContext _context;

        public AdminDashboardDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<AdminUserSignupChartDto> GetUserSignupChartAsync(DateTime fromDate, DateTime toDate)
        {
            var startDate = fromDate.Date;
            var endDate = toDate.Date;
            var endExclusive = endDate.AddDays(1);
            var (previousStartDate, previousEndExclusive) = GetPreviousPeriod(startDate, endDate);

            var dailySignups = await _context.Users
                .AsNoTracking()
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt < endExclusive
                            && u.Role != Role.Admin && u.Role != Role.Moderator)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new AdminUserSignupPointDto
                {
                    Date = g.Key,
                    SignupCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var totalSignupCount = dailySignups.Sum(x => x.SignupCount);

            var previousTotalSignupCount = await _context.Users
                .AsNoTracking()
                .Where(u => u.CreatedAt >= previousStartDate && u.CreatedAt < previousEndExclusive
                            && u.Role != Role.Admin && u.Role != Role.Moderator)
                .CountAsync();

            return new AdminUserSignupChartDto
            {
                FromDate = startDate,
                ToDate = endDate,
                TotalSignupCount = totalSignupCount,
                SignupGrowthRate = CalculateGrowthRate(totalSignupCount, previousTotalSignupCount),
                PreviousPeriod = $"từ {previousStartDate:dd-MM-yyyy} tới {previousEndExclusive.AddDays(-1):dd-MM-yyyy}",
                DailySignups = dailySignups
            };
        }

        public async Task<AdminMoneyChartDto> GetMoneyChartAsync(DateTime fromDate, DateTime toDate)
        {
            var startDate = fromDate.Date;
            var endDate = toDate.Date;
            var endExclusive = endDate.AddDays(1);
            var (previousStartDate, previousEndExclusive) = GetPreviousPeriod(startDate, endDate);

            var branchRegistrationRevenueByDate = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Status == "PAID"
                            && p.PaidAt.HasValue
                            && p.PaidAt.Value >= startDate
                            && p.PaidAt.Value < endExclusive
                            && p.BranchId.HasValue
                            && !p.OrderId.HasValue
                            && !p.BranchCampaignId.HasValue)
                .GroupBy(p => p.PaidAt!.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Amount = g.Sum(p => (decimal)p.Amount)
                })
                .ToDictionaryAsync(x => x.Date, x => x.Amount);

            var systemCampaignRevenueByDate = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Status == "PAID"
                            && p.PaidAt.HasValue
                            && p.PaidAt.Value >= startDate
                            && p.PaidAt.Value < endExclusive
                            && p.BranchCampaignId.HasValue
                            && !p.OrderId.HasValue)
                .Join(_context.BranchCampaigns.AsNoTracking(),
                    p => p.BranchCampaignId!.Value,
                    bc => bc.Id,
                    (p, bc) => new { Payment = p, BranchCampaign = bc })
                .Join(_context.Campaigns.AsNoTracking(),
                    x => x.BranchCampaign.CampaignId,
                    c => c.CampaignId,
                    (x, c) => new { x.Payment, Campaign = c })
                .Where(x => !x.Campaign.CreatedByVendorId.HasValue)
                .GroupBy(x => x.Payment.PaidAt!.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Amount = g.Sum(x => (decimal)x.Payment.Amount)
                })
                .ToDictionaryAsync(x => x.Date, x => x.Amount);

            var currentOrderCommissions = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == OrderStatus.Complete
                            && o.CreatedAt >= startDate
                            && o.CreatedAt < endExclusive)
                .Select(o => new
                {
                    o.CreatedAt,
                    o.TotalAmount,
                    o.FinalAmount,
                    CommissionRate = o.CommissionRate ?? 0m,
                    IsSystemVoucher = o.AppliedVoucherId.HasValue
                        && (o.AppliedVoucher!.VendorCampaignId == null
                            || (o.AppliedVoucher.VendorCampaign != null
                                && !o.AppliedVoucher.VendorCampaign.CreatedByVendorId.HasValue))
                })
                .ToListAsync();

            var orderCommissionByDate = currentOrderCommissions
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Amount = g.Sum(o => Math.Round((o.IsSystemVoucher ? o.FinalAmount : o.TotalAmount) * o.CommissionRate, 2, MidpointRounding.AwayFromZero))
                })
                .ToDictionary(x => x.Date, x => x.Amount);

            var dailyAmounts = branchRegistrationRevenueByDate.Keys
                .Union(systemCampaignRevenueByDate.Keys)
                .Union(orderCommissionByDate.Keys)
                .OrderBy(date => date)
                .Select(date =>
                {
                    branchRegistrationRevenueByDate.TryGetValue(date, out var branchRegistrationAmount);
                    systemCampaignRevenueByDate.TryGetValue(date, out var systemCampaignAmount);
                    orderCommissionByDate.TryGetValue(date, out var orderCommissionAmount);

                    return new AdminMoneyPointDto
                    {
                        Date = date,
                        BranchRegistrationAmount = branchRegistrationAmount,
                        SystemCampaignAmount = systemCampaignAmount,
                        OrderCommissionAmount = orderCommissionAmount
                    };
                })
                .Where(x => x.BranchRegistrationAmount > 0m || x.SystemCampaignAmount > 0m || x.OrderCommissionAmount > 0m)
                .ToList();

            var totalBranchRegistrationAmount = dailyAmounts.Sum(x => x.BranchRegistrationAmount);
            var totalSystemCampaignAmount = dailyAmounts.Sum(x => x.SystemCampaignAmount);
            var totalOrderCommissionAmount = dailyAmounts.Sum(x => x.OrderCommissionAmount);

            var previousTotalBranchRegistrationAmount = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Status == "PAID"
                            && p.PaidAt.HasValue
                            && p.PaidAt.Value >= previousStartDate
                            && p.PaidAt.Value < previousEndExclusive
                            && p.BranchId.HasValue
                            && !p.OrderId.HasValue
                            && !p.BranchCampaignId.HasValue)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var previousTotalSystemCampaignAmount = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Status == "PAID"
                            && p.PaidAt.HasValue
                            && p.PaidAt.Value >= previousStartDate
                            && p.PaidAt.Value < previousEndExclusive
                            && p.BranchCampaignId.HasValue
                            && !p.OrderId.HasValue)
                .Join(_context.BranchCampaigns.AsNoTracking(),
                    p => p.BranchCampaignId!.Value,
                    bc => bc.Id,
                    (p, bc) => new { Payment = p, BranchCampaign = bc })
                .Join(_context.Campaigns.AsNoTracking(),
                    x => x.BranchCampaign.CampaignId,
                    c => c.CampaignId,
                    (x, c) => new { x.Payment, Campaign = c })
                .Where(x => !x.Campaign.CreatedByVendorId.HasValue)
                .SumAsync(x => (decimal?)x.Payment.Amount) ?? 0m;

            var previousOrderCommissions = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == OrderStatus.Complete
                            && o.CreatedAt >= previousStartDate
                            && o.CreatedAt < previousEndExclusive)
                .Select(o => new
                {
                    o.TotalAmount,
                    o.FinalAmount,
                    CommissionRate = o.CommissionRate ?? 0m,
                    IsSystemVoucher = o.AppliedVoucherId.HasValue
                        && (o.AppliedVoucher!.VendorCampaignId == null
                            || (o.AppliedVoucher.VendorCampaign != null
                                && !o.AppliedVoucher.VendorCampaign.CreatedByVendorId.HasValue))
                })
                .ToListAsync();

            var previousTotalOrderCommissionAmount = previousOrderCommissions
                .Sum(o => Math.Round((o.IsSystemVoucher ? o.FinalAmount : o.TotalAmount) * o.CommissionRate, 2, MidpointRounding.AwayFromZero));

            return new AdminMoneyChartDto
            {
                FromDate = startDate,
                ToDate = endDate,
                TotalBranchRegistrationAmount = totalBranchRegistrationAmount,
                TotalSystemCampaignAmount = totalSystemCampaignAmount,
                TotalOrderCommissionAmount = totalOrderCommissionAmount,
                BranchRegistrationGrowthRate = CalculateGrowthRate(totalBranchRegistrationAmount, previousTotalBranchRegistrationAmount),
                SystemCampaignGrowthRate = CalculateGrowthRate(totalSystemCampaignAmount, previousTotalSystemCampaignAmount),
                OrderCommissionGrowthRate = CalculateGrowthRate(totalOrderCommissionAmount, previousTotalOrderCommissionAmount),
                PreviousPeriod = $"từ {previousStartDate:dd-MM-yyyy} tới {previousEndExclusive.AddDays(-1):dd-MM-yyyy}",
                DailyAmounts = dailyAmounts
            };
        }

        public async Task<AdminCompensationChartDto> GetCompensationChartAsync(DateTime fromDate, DateTime toDate)
        {
            var startDate = fromDate.Date;
            var endDate = toDate.Date;
            var endExclusive = endDate.AddDays(1);
            var (previousStartDate, previousEndExclusive) = GetPreviousPeriod(startDate, endDate);

            var systemVoucherCompensableOrdersQuery = GetSystemVoucherCompensableOrdersQuery(startDate, endExclusive);

            var systemVoucherCompensationByDate = await systemVoucherCompensableOrdersQuery
                .GroupBy(o => o.UpdatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Amount = g.Sum(o => o.DiscountAmount ?? 0m)
                })
                .ToDictionaryAsync(x => x.Date, x => x.Amount);

            var compensationByVendors = await systemVoucherCompensableOrdersQuery
                .Where(o => o.Branch.VendorId.HasValue)
                .GroupBy(o => new
                {
                    VendorId = o.Branch.VendorId!.Value,
                    VendorName = o.Branch.Vendor!.Name
                })
                .Select(g => new AdminVendorCompensationDto
                {
                    VendorId = g.Key.VendorId,
                    VendorName = g.Key.VendorName,
                    CompensationAmount = g.Sum(o => o.DiscountAmount ?? 0m)
                })
                .Where(x => x.CompensationAmount > 0m)
                .OrderByDescending(x => x.CompensationAmount)
                .ThenBy(x => x.VendorName)
                .ToListAsync();

            var dailyCompensations = systemVoucherCompensationByDate
                .Where(x => x.Value > 0m)
                .OrderBy(x => x.Key)
                .Select(x => new AdminCompensationPointDto
                {
                    Date = x.Key,
                    CompensationAmount = x.Value
                })
                .ToList();

            var totalCompensationAmount = dailyCompensations.Sum(x => x.CompensationAmount);

            var previousTotalCompensationAmount = await GetSystemVoucherCompensableOrdersQuery(previousStartDate, previousEndExclusive)
                .SumAsync(o => o.DiscountAmount) ?? 0m;

            return new AdminCompensationChartDto
            {
                FromDate = startDate,
                ToDate = endDate,
                TotalCompensationAmount = totalCompensationAmount,
                CompensationGrowthRate = CalculateGrowthRate(totalCompensationAmount, previousTotalCompensationAmount),
                PreviousPeriod = $"từ {previousStartDate:dd-MM-yyyy} tới {previousEndExclusive.AddDays(-1):dd-MM-yyyy}",
                DailyCompensations = dailyCompensations,
                CompensationByVendors = compensationByVendors
            };
        }

        public async Task<AdminUserToVendorConversionChartDto> GetUserToVendorConversionChartAsync(DateTime fromDate, DateTime toDate)
        {
            var startDate = fromDate.Date;
            var endDate = toDate.Date;
            var endExclusive = endDate.AddDays(1);
            var (previousStartDate, previousEndExclusive) = GetPreviousPeriod(startDate, endDate);

            var dailyConversions = await _context.Vendors
                .AsNoTracking()
                .Where(v => v.CreatedAt >= startDate && v.CreatedAt < endExclusive)
                .GroupBy(v => v.CreatedAt.Date)
                .Select(g => new AdminUserToVendorConversionPointDto
                {
                    Date = g.Key,
                    ConversionCount = g.Select(v => v.UserId).Distinct().Count()
                })
                .Where(x => x.ConversionCount > 0)
                .OrderBy(x => x.Date)
                .ToListAsync();

            var totalConversionCount = dailyConversions.Sum(x => x.ConversionCount);

            var previousTotalConversionCount = await _context.Vendors
                .AsNoTracking()
                .Where(v => v.CreatedAt >= previousStartDate && v.CreatedAt < previousEndExclusive)
                .Select(v => v.UserId)
                .Distinct()
                .CountAsync();

            return new AdminUserToVendorConversionChartDto
            {
                FromDate = startDate,
                ToDate = endDate,
                TotalConversionCount = totalConversionCount,
                ConversionGrowthRate = CalculateGrowthRate(totalConversionCount, previousTotalConversionCount),
                PreviousPeriod = $"từ {previousStartDate:dd-MM-yyyy} tới {previousEndExclusive.AddDays(-1):dd-MM-yyyy}",
                DailyConversions = dailyConversions
            };
        }

        private IQueryable<Order> GetSystemVoucherCompensableOrdersQuery(DateTime periodStart, DateTime periodEndExclusive)
        {
            return _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == OrderStatus.Complete
                            && o.AppliedVoucherId.HasValue
                            && o.UpdatedAt >= periodStart
                            && o.UpdatedAt < periodEndExclusive
                            && o.AppliedVoucher!.UserVouchers.Any(uv => uv.UserId == o.UserId)
                            && (o.AppliedVoucher!.VendorCampaignId == null
                                || (o.AppliedVoucher.VendorCampaign != null
                                    && !o.AppliedVoucher.VendorCampaign.CreatedByVendorId.HasValue)));
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

        public async Task<List<AdminSystemCampaignDetailsDto>> GetAllSystemCampaignDetailsAsync()
        {
            var campaigns = await _context.Campaigns
                .AsNoTracking()
                .Where(c => c.CreatedByVendorId == null)
                .ToListAsync();

            var result = new List<AdminSystemCampaignDetailsDto>();

            foreach (var campaign in campaigns)
            {
                var campaignId = campaign.CampaignId;

                var campaignVoucherIds = await _context.Vouchers
                    .AsNoTracking()
                    .Where(v => v.VendorCampaignId == campaignId
                                || _context.QuestTaskRewards.Any(qtr => qtr.RewardType == BO.Enums.QuestRewardType.VOUCHER
                                                                        && qtr.RewardValue == v.VoucherId
                                                                        && qtr.QuestTask.Quest.CampaignId == campaignId))
                    .Select(v => v.VoucherId)
                    .ToListAsync();

                var totalBranchesJoined = await _context.BranchCampaigns
                    .AsNoTracking()
                    .Where(bc => bc.CampaignId == campaignId && bc.IsActive)
                    .CountAsync();

                var branchOrdersQuery = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.Status == OrderStatus.Complete 
                                && o.AppliedVoucherId.HasValue
                                && campaignVoucherIds.Contains(o.AppliedVoucherId.Value)
                                && _context.BranchCampaigns.Any(bc => bc.CampaignId == campaignId
                                                                    && bc.BranchId == o.BranchId
                                                                    && bc.IsActive))
                    .GroupBy(o => new { o.BranchId, o.Branch.Name })
                    .Select(g => new AdminSystemCampaignBranchOrderDto
                    {
                        BranchId = g.Key.BranchId,
                        BranchName = g.Key.Name,
                        OrderCount = g.Count()
                    })
                    .ToListAsync();

                var totalOrders = branchOrdersQuery.Sum(bo => bo.OrderCount);

                var questsQuery = await _context.Quests
                    .AsNoTracking()
                    .Where(q => q.CampaignId == campaignId)
                    .Select(q => new
                    {
                        q.QuestId,
                        q.Title,
                        TotalUsersDoing = q.UserQuests.Count,
                        UsersCurrentlyDoing = q.UserQuests.Count(uq => uq.Status == "IN_PROGRESS"),
                        UsersFinished = q.UserQuests.Count(uq => uq.Status == "COMPLETED")
                    })
                    .ToListAsync();

                var questsDto = questsQuery.Select(q => new AdminSystemCampaignQuestDto
                {
                    QuestId = q.QuestId,
                    QuestTitle = q.Title,
                    TotalUsersDoing = q.TotalUsersDoing,
                    UsersCurrentlyDoing = q.UsersCurrentlyDoing,
                    UsersFinished = q.UsersFinished
                }).ToList();

                var vouchersQuery = await _context.Vouchers
                    .AsNoTracking()
                    .Where(v => campaignVoucherIds.Contains(v.VoucherId))
                    .Select(v => new AdminSystemCampaignVoucherDto
                    {
                        VoucherId = v.VoucherId,
                        VoucherName = v.Name,
                        TotalUsed = _context.Orders.Count(o => o.Status == OrderStatus.Complete && o.AppliedVoucherId == v.VoucherId)
                    })
                    .ToListAsync();

                var campaignOrders = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.Status == OrderStatus.Complete 
                                && o.AppliedVoucherId.HasValue
                                && campaignVoucherIds.Contains(o.AppliedVoucherId.Value)
                                && _context.BranchCampaigns.Any(bc => bc.CampaignId == campaignId
                                                                    && bc.BranchId == o.BranchId
                                                                    && bc.IsActive))
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new AdminSystemCampaignOrderDto
                    {
                        OrderId = o.OrderId,
                        BranchName = o.Branch.Name,
                        VoucherName = o.AppliedVoucher!.Name,
                        TotalAmount = o.TotalAmount,
                        DiscountAmount = o.DiscountAmount ?? 0m,
                        CreatedAt = o.CreatedAt
                    })
                    .ToListAsync();

                result.Add(new AdminSystemCampaignDetailsDto
                {
                    CampaignId = campaign.CampaignId,
                    CampaignName = campaign.Name,
                    TotalBranchesJoined = totalBranchesJoined,
                    TotalOrders = totalOrders,
                    BranchOrders = branchOrdersQuery,
                    Quests = questsDto,
                    Vouchers = vouchersQuery,
                    CampaignOrders = campaignOrders
                });
            }

            return result;
        }

        public async Task<RevenueBarChartDto> GetRevenueBarChartAsync(DateTime fromDate, DateTime toDate, DateTime? previousFromDate = null, DateTime? previousToDate = null)
        {
            var startDate = fromDate.Date;
            var endDate = toDate.Date;
            var endExclusive = endDate.AddDays(1);
            
            // Use provided previous dates if available, otherwise auto-calculate
            DateTime previousStartDate;
            DateTime previousEndExclusive;
            
            if (previousFromDate.HasValue && previousToDate.HasValue)
            {
                previousStartDate = previousFromDate.Value.Date;
                previousEndExclusive = previousToDate.Value.Date.AddDays(1);
            }
            else
            {
                (previousStartDate, previousEndExclusive) = GetPreviousPeriod(startDate, endDate);
            }

            var result = new RevenueBarChartDto();

            var currentTotal = await GetOrderCommissionTotalAsync(startDate, endExclusive);
            var previousTotal = await GetOrderCommissionTotalAsync(previousStartDate, previousEndExclusive);

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

        private async Task<decimal> GetOrderCommissionTotalAsync(DateTime periodStart, DateTime periodEndExclusive)
        {
            var orderCommissions = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == OrderStatus.Complete
                            && o.CreatedAt >= periodStart
                            && o.CreatedAt < periodEndExclusive)
                .Select(o => new
                {
                    o.TotalAmount,
                    o.FinalAmount,
                    CommissionRate = o.CommissionRate ?? 0m,
                    IsSystemVoucher = o.AppliedVoucherId.HasValue
                        && (o.AppliedVoucher!.VendorCampaignId == null
                            || (o.AppliedVoucher.VendorCampaign != null
                                && !o.AppliedVoucher.VendorCampaign.CreatedByVendorId.HasValue))
                })
                .ToListAsync();

            return orderCommissions
                .Sum(o => Math.Round((o.IsSystemVoucher ? o.FinalAmount : o.TotalAmount) * o.CommissionRate, 2, MidpointRounding.AwayFromZero));
        }
    }
}
