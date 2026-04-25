using BO.DTO.Dashboard;
using System;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IVendorDashboardRepository
    {
        Task<int?> GetVendorIdByUserIdAsync(int userId);
        Task<RevenueDashboardDto> GetRevenueDashboardAsync(int vendorId, DateTime fromDate, DateTime toDate);
        Task<CampaignDashboardDto> GetCampaignDashboardAsync(int vendorId, DateTime fromDate, DateTime toDate);
        Task<VoucherDashboardDto> GetVoucherDashboardAsync(int vendorId);
        Task<DishDashboardDto> GetDishDashboardAsync(int vendorId);
    }
}
