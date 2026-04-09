using BO.DTO.Dashboard;
using System;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IAdminDashboardRepository
    {
        Task<AdminUserSignupChartDto> GetUserSignupChartAsync(DateTime fromDate, DateTime toDate);
        Task<AdminMoneyChartDto> GetMoneyChartAsync(DateTime fromDate, DateTime toDate);
        Task<AdminCompensationChartDto> GetCompensationChartAsync(DateTime fromDate, DateTime toDate);
    }
}
