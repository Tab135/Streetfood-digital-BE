using BO.DTO.Dashboard;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace Service
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IAdminDashboardRepository _adminDashboardRepository;

        public AdminDashboardService(IAdminDashboardRepository adminDashboardRepository)
        {
            _adminDashboardRepository = adminDashboardRepository ?? throw new ArgumentNullException(nameof(adminDashboardRepository));
        }

        public async Task<AdminUserSignupChartDto> GetUserSignupChartAsync(DateTime fromDate, DateTime toDate)
        {
            ValidateDateRange(fromDate, toDate);
            return await _adminDashboardRepository.GetUserSignupChartAsync(fromDate, toDate);
        }

        public async Task<AdminMoneyChartDto> GetMoneyChartAsync(DateTime fromDate, DateTime toDate)
        {
            ValidateDateRange(fromDate, toDate);
            return await _adminDashboardRepository.GetMoneyChartAsync(fromDate, toDate);
        }

        public async Task<AdminCompensationChartDto> GetCompensationChartAsync(DateTime fromDate, DateTime toDate)
        {
            ValidateDateRange(fromDate, toDate);
            return await _adminDashboardRepository.GetCompensationChartAsync(fromDate, toDate);
        }

        private static void ValidateDateRange(DateTime fromDate, DateTime toDate)
        {
            if (fromDate == default || toDate == default)
            {
                throw new DomainExceptions("fromDate and toDate are required.", "INVALID_DATE_RANGE");
            }

            if (fromDate.Date > toDate.Date)
            {
                throw new DomainExceptions("fromDate must be less than or equal to toDate.", "INVALID_DATE_RANGE");
            }
        }

    }
}
