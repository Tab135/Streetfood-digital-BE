using BO.DTO.Dashboard;
using System;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IVendorDashboardService
    {
        Task<RevenueDashboardDto> GetRevenueDashboardAsync(int userId, DateTime fromDate, DateTime toDate);
        Task<VoucherDashboardDto> GetVoucherDashboardAsync(int userId);
        Task<DishDashboardDto> GetDishDashboardAsync(int userId);
    }
}
