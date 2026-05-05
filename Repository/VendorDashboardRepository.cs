using BO.DTO.Dashboard;
using DAL;
using Repository.Interfaces;
using System;
using System.Threading.Tasks;

namespace Repository
{
    public class VendorDashboardRepository : IVendorDashboardRepository
    {
        private readonly VendorDashboardDAO _vendorDashboardDao;

        public VendorDashboardRepository(VendorDashboardDAO vendorDashboardDao)
        {
            _vendorDashboardDao = vendorDashboardDao ?? throw new ArgumentNullException(nameof(vendorDashboardDao));
        }

        public Task<(int? vendorId, System.Collections.Generic.List<int>? allowedBranchIds)> GetDashboardContextByUserIdAsync(int userId)
            => _vendorDashboardDao.GetDashboardContextByUserIdAsync(userId);

        public Task<RevenueDashboardDto> GetRevenueDashboardAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate)
            => _vendorDashboardDao.GetRevenueDashboardAsync(vendorId, allowedBranchIds, fromDate, toDate);

        public Task<CampaignDashboardDto> GetCampaignDashboardAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate)
            => _vendorDashboardDao.GetCampaignDashboardAsync(vendorId, allowedBranchIds, fromDate, toDate);

        public Task<VoucherDashboardDto> GetVoucherDashboardAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate)
            => _vendorDashboardDao.GetVoucherDashboardAsync(vendorId, allowedBranchIds, fromDate, toDate);

        public Task<DishDashboardDto> GetDishDashboardAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate)
            => _vendorDashboardDao.GetDishDashboardAsync(vendorId, allowedBranchIds, fromDate, toDate);

        public Task<RevenueBarChartDto> GetRevenueBarChartAsync(int vendorId, System.Collections.Generic.List<int>? allowedBranchIds, DateTime fromDate, DateTime toDate)
            => _vendorDashboardDao.GetRevenueBarChartAsync(vendorId, allowedBranchIds, fromDate, toDate);
    }
}

