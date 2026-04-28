using BO.DTO.Dashboard;
using System;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IVendorDashboardService
    {
        Task<RevenueDashboardDto> GetRevenueDashboardAsync(int userId, DateTime fromDate, DateTime toDate);
        Task<CampaignDashboardDto> GetCampaignDashboardAsync(int userId, DateTime fromDate, DateTime toDate);
        Task<VoucherDashboardDto> GetVoucherDashboardAsync(int userId);
        Task<DishDashboardDto> GetDishDashboardAsync(int userId);

        /// <summary>
        /// Returns the dish dashboard for a vendor addressed directly by vendorId
        /// (no user-to-vendor resolution). Used by search ranking to look up a vendor's
        /// best-sellers without requiring the caller to know the owning manager's userId.
        /// </summary>
        Task<DishDashboardDto> GetDishDashboardByVendorAsync(int vendorId);
    }
}
