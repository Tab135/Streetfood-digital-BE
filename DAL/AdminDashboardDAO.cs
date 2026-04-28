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

            var dailyAmounts = branchRegistrationRevenueByDate.Keys
                .Union(systemCampaignRevenueByDate.Keys)
                .OrderBy(date => date)
                .Select(date =>
                {
                    branchRegistrationRevenueByDate.TryGetValue(date, out var branchRegistrationAmount);
                    systemCampaignRevenueByDate.TryGetValue(date, out var systemCampaignAmount);

                    return new AdminMoneyPointDto
                    {
                        Date = date,
                        BranchRegistrationAmount = branchRegistrationAmount,
                        SystemCampaignAmount = systemCampaignAmount
                    };
                })
                .Where(x => x.BranchRegistrationAmount > 0m || x.SystemCampaignAmount > 0m)
                .ToList();

            var totalBranchRegistrationAmount = dailyAmounts.Sum(x => x.BranchRegistrationAmount);
            var totalSystemCampaignAmount = dailyAmounts.Sum(x => x.SystemCampaignAmount);

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

            return new AdminMoneyChartDto
            {
                FromDate = startDate,
                ToDate = endDate,
                TotalBranchRegistrationAmount = totalBranchRegistrationAmount,
                TotalSystemCampaignAmount = totalSystemCampaignAmount,
                BranchRegistrationGrowthRate = CalculateGrowthRate(totalBranchRegistrationAmount, previousTotalBranchRegistrationAmount),
                SystemCampaignGrowthRate = CalculateGrowthRate(totalSystemCampaignAmount, previousTotalSystemCampaignAmount),
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
    }
}
