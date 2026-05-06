using BO.DTO.Dashboard;
using System;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IVendorDashboardRepository
    {
        Task<(int? vendorId, System.Collections.Generic.List<int>? allowedBranchIds)> GetDashboardContextByUserIdAsync(int userId);
        Task<RevenueDashboardDto> GetRevenueDashboardAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate);
        Task<CampaignDashboardDto> GetCampaignDashboardAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate);
        Task<VoucherDashboardDto> GetVoucherDashboardAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate);
        Task<DishDashboardDto> GetDishDashboardAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate);
        Task<RevenueBarChartDto> GetRevenueBarChartAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate);
        Task<BranchesPerformanceDashboardDto> GetBranchesPerformanceAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate);
        Task<decimal> GetCommissionRateAsync();
    }
}


