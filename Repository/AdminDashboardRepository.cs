using BO.DTO.Dashboard;
using DAL;
using Repository.Interfaces;
using System;
using System.Threading.Tasks;

namespace Repository
{
    public class AdminDashboardRepository : IAdminDashboardRepository
    {
        private readonly AdminDashboardDAO _adminDashboardDAO;

        public AdminDashboardRepository(AdminDashboardDAO adminDashboardDAO)
        {
            _adminDashboardDAO = adminDashboardDAO ?? throw new ArgumentNullException(nameof(adminDashboardDAO));
        }

        public async Task<AdminUserSignupChartDto> GetUserSignupChartAsync(DateTime fromDate, DateTime toDate)
        {
            return await _adminDashboardDAO.GetUserSignupChartAsync(fromDate, toDate);
        }

        public async Task<AdminMoneyChartDto> GetMoneyChartAsync(DateTime fromDate, DateTime toDate)
        {
            return await _adminDashboardDAO.GetMoneyChartAsync(fromDate, toDate);
        }

        public async Task<AdminCompensationChartDto> GetCompensationChartAsync(DateTime fromDate, DateTime toDate)
        {
            return await _adminDashboardDAO.GetCompensationChartAsync(fromDate, toDate);
        }

        public async Task<AdminUserToVendorConversionChartDto> GetUserToVendorConversionChartAsync(DateTime fromDate, DateTime toDate)
        {
            return await _adminDashboardDAO.GetUserToVendorConversionChartAsync(fromDate, toDate);
        }
    }
}
