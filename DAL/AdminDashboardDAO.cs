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


        private static readonly TimeZoneInfo VietnamTz =
            TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");

        private static DateTime NormalizeToUtcDayStart(DateTime dt)
        {
            // Ensure the DateTime is treated as UTC before converting
            var utcDt = dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                : dt.ToUniversalTime();

            // Convert to Vietnam local time, take the local date, convert back to UTC
            var vnLocal = TimeZoneInfo.ConvertTimeFromUtc(utcDt, VietnamTz);
            var vnDayStart = vnLocal.Date; // e.g. 2026-05-01 00:00:00 (Vietnam)
            return TimeZoneInfo.ConvertTimeToUtc(vnDayStart, VietnamTz); // e.g. 2026-04-30T17:00:00Z
        }

        public async Task<AdminUserSignupChartDto> GetUserSignupChartAsync(DateTime fromDate, DateTime toDate)
        {
            fromDate = NormalizeToUtcDayStart(fromDate);
            toDate = NormalizeToUtcDayStart(toDate);

            var duration = toDate - fromDate;
            var previousEnd = fromDate;
            var previousStart = fromDate.Add(-duration);

            var dailySignups = await _context.Users
                .AsNoTracking()
                .Where(u => u.CreatedAt >= fromDate && u.CreatedAt < toDate
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
                .Where(u => u.CreatedAt >= previousStart && u.CreatedAt < previousEnd
                            && u.Role != Role.Admin && u.Role != Role.Moderator)
                .CountAsync();

            return new AdminUserSignupChartDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalSignupCount = totalSignupCount,
                SignupGrowthRate = CalculateGrowthRate(totalSignupCount, previousTotalSignupCount),
                PreviousPeriod = $"từ {previousStart:dd-MM-yyyy} tới {previousEnd:dd-MM-yyyy}",
                DailySignups = dailySignups
            };
        }

        public async Task<AdminMoneyChartDto> GetMoneyChartAsync(DateTime fromDate, DateTime toDate)
        {
            fromDate = NormalizeToUtcDayStart(fromDate);
            toDate = NormalizeToUtcDayStart(toDate);

            var duration = toDate - fromDate;
            var previousEnd = fromDate;
            var previousStart = fromDate.Add(-duration);

            var branchRegistrationRevenueByDate = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Status == "PAID"
                            && p.PaidAt.HasValue
                            && p.PaidAt.Value >= fromDate
                            && p.PaidAt.Value < toDate
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
                            && p.PaidAt.Value >= fromDate
                            && p.PaidAt.Value < toDate
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
                            && o.CreatedAt >= fromDate
                            && o.CreatedAt < toDate)
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
                    Amount = g.Sum(o => Math.Round(
                        (o.IsSystemVoucher ? o.FinalAmount : o.TotalAmount) * o.CommissionRate,
                        2, MidpointRounding.AwayFromZero))
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
                            && p.PaidAt.Value >= previousStart
                            && p.PaidAt.Value < previousEnd
                            && p.BranchId.HasValue
                            && !p.OrderId.HasValue
                            && !p.BranchCampaignId.HasValue)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var previousTotalSystemCampaignAmount = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Status == "PAID"
                            && p.PaidAt.HasValue
                            && p.PaidAt.Value >= previousStart
                            && p.PaidAt.Value < previousEnd
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
                            && o.CreatedAt >= previousStart
                            && o.CreatedAt < previousEnd)
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
                .Sum(o => Math.Round(
                    (o.IsSystemVoucher ? o.FinalAmount : o.TotalAmount) * o.CommissionRate,
                    2, MidpointRounding.AwayFromZero));

            return new AdminMoneyChartDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalBranchRegistrationAmount = totalBranchRegistrationAmount,
                TotalSystemCampaignAmount = totalSystemCampaignAmount,
                TotalOrderCommissionAmount = totalOrderCommissionAmount,
                BranchRegistrationGrowthRate = CalculateGrowthRate(totalBranchRegistrationAmount, previousTotalBranchRegistrationAmount),
                SystemCampaignGrowthRate = CalculateGrowthRate(totalSystemCampaignAmount, previousTotalSystemCampaignAmount),
                OrderCommissionGrowthRate = CalculateGrowthRate(totalOrderCommissionAmount, previousTotalOrderCommissionAmount),
                PreviousPeriod = $"từ {previousStart:dd-MM-yyyy} tới {previousEnd:dd-MM-yyyy}",
                DailyAmounts = dailyAmounts
            };
        }

        public async Task<AdminCompensationChartDto> GetCompensationChartAsync(DateTime fromDate, DateTime toDate)
        {
            fromDate = NormalizeToUtcDayStart(fromDate);
            toDate = NormalizeToUtcDayStart(toDate);

            var duration = toDate - fromDate;
            var previousEnd = fromDate;
            var previousStart = fromDate.Add(-duration);

            var systemVoucherCompensableOrdersQuery = GetSystemVoucherCompensableOrdersQuery(fromDate, toDate);

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

            // FIXED: removed duplicate declaration and undefined variable references
            var previousTotalCompensationAmount = await GetSystemVoucherCompensableOrdersQuery(previousStart, previousEnd)
                .SumAsync(o => (decimal?)o.DiscountAmount) ?? 0m;

            return new AdminCompensationChartDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalCompensationAmount = totalCompensationAmount,
                CompensationGrowthRate = CalculateGrowthRate(totalCompensationAmount, previousTotalCompensationAmount),
                PreviousPeriod = $"từ {previousStart:dd-MM-yyyy} tới {previousEnd:dd-MM-yyyy}",
                DailyCompensations = dailyCompensations,
                CompensationByVendors = compensationByVendors
            };
        }

        public async Task<AdminUserToVendorConversionChartDto> GetUserToVendorConversionChartAsync(DateTime fromDate, DateTime toDate)
        {
            fromDate = NormalizeToUtcDayStart(fromDate);
            toDate = NormalizeToUtcDayStart(toDate);

            var duration = toDate - fromDate;
            var previousEnd = fromDate;
            var previousStart = fromDate.Add(-duration);

            var dailyConversions = await _context.Vendors
                .AsNoTracking()
                .Where(v => v.CreatedAt >= fromDate && v.CreatedAt < toDate)
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
                .Where(v => v.CreatedAt >= previousStart && v.CreatedAt < previousEnd)
                .Select(v => v.UserId)
                .Distinct()
                .CountAsync();

            return new AdminUserToVendorConversionChartDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalConversionCount = totalConversionCount,
                ConversionGrowthRate = CalculateGrowthRate(totalConversionCount, previousTotalConversionCount),
                PreviousPeriod = $"từ {previousStart:dd-MM-yyyy} tới {previousEnd:dd-MM-yyyy}",
                DailyConversions = dailyConversions
            };
        }

        public async Task<RevenueBarChartDto> GetRevenueBarChartAsync(DateTime fromDate, DateTime toDate, DateTime? previousFromDate = null, DateTime? previousToDate = null)
        {
            fromDate = NormalizeToUtcDayStart(fromDate);
            toDate = NormalizeToUtcDayStart(toDate);

            DateTime previousStart;
            DateTime previousEnd;

            if (previousFromDate.HasValue && previousToDate.HasValue)
            {
                previousStart = NormalizeToUtcDayStart(previousFromDate.Value);
                previousEnd = NormalizeToUtcDayStart(previousToDate.Value);
            }
            else
            {
                var duration = toDate - fromDate;
                previousEnd = fromDate;
                previousStart = fromDate.Add(-duration);
            }

            var result = new RevenueBarChartDto();

            var currentTotal = await GetMoneyTotalAsync(fromDate, toDate);
            var previousTotal = await GetMoneyTotalAsync(previousStart, previousEnd);

            result.Items.Add(new BarChartItemDto
            {
                Label = "Previous",
                FromDate = previousStart,
                ToDate = previousEnd,
                Value = previousTotal
            });

            result.Items.Add(new BarChartItemDto
            {
                Label = "Now",
                FromDate = fromDate,
                ToDate = toDate,
                Value = currentTotal
            });

            return result;
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
                                || _context.QuestTaskRewards.Any(qtr =>
                                    qtr.RewardType == BO.Enums.QuestRewardType.VOUCHER
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
                                && _context.BranchCampaigns.Any(bc =>
                                    bc.CampaignId == campaignId
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
                        TotalUsed = _context.Orders.Count(o =>
                            o.Status == OrderStatus.Complete && o.AppliedVoucherId == v.VoucherId)
                    })
                    .ToListAsync();

                var campaignOrders = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.Status == OrderStatus.Complete
                                && o.AppliedVoucherId.HasValue
                                && campaignVoucherIds.Contains(o.AppliedVoucherId.Value)
                                && _context.BranchCampaigns.Any(bc =>
                                    bc.CampaignId == campaignId
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

        private async Task<decimal> GetMoneyTotalAsync(DateTime periodStart, DateTime periodEndExclusive)
        {
            var branchRegistrationTotal = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Status == "PAID"
                            && p.PaidAt.HasValue
                            && p.PaidAt.Value >= periodStart
                            && p.PaidAt.Value < periodEndExclusive
                            && p.BranchId.HasValue
                            && !p.OrderId.HasValue
                            && !p.BranchCampaignId.HasValue)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var systemCampaignTotal = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Status == "PAID"
                            && p.PaidAt.HasValue
                            && p.PaidAt.Value >= periodStart
                            && p.PaidAt.Value < periodEndExclusive
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

            var totalOrderCommissionAmount = orderCommissions
                .Sum(o => Math.Round(
                    (o.IsSystemVoucher ? o.FinalAmount : o.TotalAmount) * o.CommissionRate,
                    2, MidpointRounding.AwayFromZero));

            return branchRegistrationTotal + systemCampaignTotal + totalOrderCommissionAmount;
        }

        private static decimal CalculateGrowthRate(decimal currentValue, decimal previousValue)
        {
            if (previousValue == 0m)
            {
                return currentValue == 0m ? 0m : 100m;
            }

            return Math.Round(((currentValue - previousValue) / previousValue) * 100m, 2);
        }
    }
}