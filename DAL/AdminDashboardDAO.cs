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

            return new AdminUserSignupChartDto
            {
                FromDate = startDate,
                ToDate = endDate,
                TotalSignupCount = dailySignups.Sum(x => x.SignupCount),
                DailySignups = dailySignups
            };
        }

        public async Task<AdminMoneyChartDto> GetMoneyChartAsync(DateTime fromDate, DateTime toDate)
        {
            var startDate = fromDate.Date;
            var endDate = toDate.Date;
            var endExclusive = endDate.AddDays(1);

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

            return new AdminMoneyChartDto
            {
                FromDate = startDate,
                ToDate = endDate,
                TotalBranchRegistrationAmount = dailyAmounts.Sum(x => x.BranchRegistrationAmount),
                TotalSystemCampaignAmount = dailyAmounts.Sum(x => x.SystemCampaignAmount),
                DailyAmounts = dailyAmounts
            };
        }

        public async Task<AdminCompensationChartDto> GetCompensationChartAsync(DateTime fromDate, DateTime toDate)
        {
            var startDate = fromDate.Date;
            var endDate = toDate.Date;
            var endExclusive = endDate.AddDays(1);

            var systemVoucherCompensationByDate = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == OrderStatus.Complete
                            && o.AppliedVoucherId.HasValue
                            && o.UpdatedAt >= startDate
                            && o.UpdatedAt < endExclusive
                            && o.AppliedVoucher!.UserVouchers.Any(uv => uv.UserId == o.UserId)
                            && (o.AppliedVoucher!.VendorCampaignId == null
                                || (o.AppliedVoucher.VendorCampaign != null
                                    && !o.AppliedVoucher.VendorCampaign.CreatedByVendorId.HasValue)))
                .GroupBy(o => o.UpdatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Amount = g.Sum(o => o.DiscountAmount ?? 0m)
                })
                .ToDictionaryAsync(x => x.Date, x => x.Amount);

            var dailyCompensations = systemVoucherCompensationByDate
                .Where(x => x.Value > 0m)
                .OrderBy(x => x.Key)
                .Select(x => new AdminCompensationPointDto
                {
                    Date = x.Key,
                    CompensationAmount = x.Value
                })
                .ToList();

            return new AdminCompensationChartDto
            {
                FromDate = startDate,
                ToDate = endDate,
                TotalCompensationAmount = dailyCompensations.Sum(x => x.CompensationAmount),
                DailyCompensations = dailyCompensations
            };
        }

        public async Task<AdminUserToVendorConversionChartDto> GetUserToVendorConversionChartAsync(DateTime fromDate, DateTime toDate)
        {
            var startDate = fromDate.Date;
            var endDate = toDate.Date;
            var endExclusive = endDate.AddDays(1);

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

            return new AdminUserToVendorConversionChartDto
            {
                FromDate = startDate,
                ToDate = endDate,
                TotalConversionCount = dailyConversions.Sum(x => x.ConversionCount),
                DailyConversions = dailyConversions
            };
        }
    }
}
