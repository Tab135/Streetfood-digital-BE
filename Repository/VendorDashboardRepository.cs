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

        public Task<int?> GetVendorIdByUserIdAsync(int userId)
            => _vendorDashboardDao.GetVendorIdByUserIdAsync(userId);

        public Task<RevenueDashboardDto> GetRevenueDashboardAsync(int vendorId, DateTime fromDate, DateTime toDate)
            => _vendorDashboardDao.GetRevenueDashboardAsync(vendorId, fromDate, toDate);

        public Task<CampaignDashboardDto> GetCampaignDashboardAsync(int vendorId, DateTime fromDate, DateTime toDate)
            => _vendorDashboardDao.GetCampaignDashboardAsync(vendorId, fromDate, toDate);

        public Task<VoucherDashboardDto> GetVoucherDashboardAsync(int vendorId, DateTime fromDate, DateTime toDate)
            => _vendorDashboardDao.GetVoucherDashboardAsync(vendorId, fromDate, toDate);

        public Task<DishDashboardDto> GetDishDashboardAsync(int vendorId, DateTime fromDate, DateTime toDate)
            => _vendorDashboardDao.GetDishDashboardAsync(vendorId, fromDate, toDate);

        public Task<RevenueBarChartDto> GetRevenueBarChartAsync(int vendorId, DateTime fromDate, DateTime toDate)
            => _vendorDashboardDao.GetRevenueBarChartAsync(vendorId, fromDate, toDate);
    }
}
