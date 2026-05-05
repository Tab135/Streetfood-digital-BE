using BO.DTO.Dashboard;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace Service
{
    public class VendorDashboardService : IVendorDashboardService
    {
        private readonly IVendorDashboardRepository _vendorDashboardRepo;

        public VendorDashboardService(IVendorDashboardRepository vendorDashboardRepo)
        {
            _vendorDashboardRepo = vendorDashboardRepo ?? throw new ArgumentNullException(nameof(vendorDashboardRepo));
        }

        public async Task<RevenueDashboardDto> GetRevenueDashboardAsync(int userId, DateTime fromDate, DateTime toDate)
        {
            var context = await _vendorDashboardRepo.GetDashboardContextByUserIdAsync(userId);
            if (context.vendorId == null)
            {
                throw new DomainExceptions("Không tìm thấy Vendor hoặc Manager cho nguời dùng này.", "VENDOR_OR_MANAGER_NOT_FOUND");
            }

            return await _vendorDashboardRepo.GetRevenueDashboardAsync(context.vendorId.Value, context.allowedBranchIds, fromDate, toDate);
        }

        public async Task<CampaignDashboardDto> GetCampaignDashboardAsync(int userId, DateTime fromDate, DateTime toDate)
        {
            var context = await _vendorDashboardRepo.GetDashboardContextByUserIdAsync(userId);
            if (context.vendorId == null)
            {
                throw new DomainExceptions("Không tìm thấy Vendor hoặc Manager cho nguời dùng này.", "VENDOR_OR_MANAGER_NOT_FOUND");
            }

            return await _vendorDashboardRepo.GetCampaignDashboardAsync(context.vendorId.Value, context.allowedBranchIds, fromDate, toDate);
        }

        public async Task<VoucherDashboardDto> GetVoucherDashboardAsync(int userId, DateTime fromDate, DateTime toDate)
        {
            var context = await _vendorDashboardRepo.GetDashboardContextByUserIdAsync(userId);
            if (context.vendorId == null)
            {
                throw new DomainExceptions("Không tìm thấy Vendor hoặc Manager cho nguời dùng này.", "VENDOR_OR_MANAGER_NOT_FOUND");
            }

            return await _vendorDashboardRepo.GetVoucherDashboardAsync(context.vendorId.Value, context.allowedBranchIds, fromDate, toDate);
        }

        public async Task<DishDashboardDto> GetDishDashboardAsync(int userId, DateTime fromDate, DateTime toDate)
        {
            var context = await _vendorDashboardRepo.GetDashboardContextByUserIdAsync(userId);
            if (context.vendorId == null)
            {
                throw new DomainExceptions("Không tìm thấy Vendor hoặc Manager cho nguời dùng này.", "VENDOR_OR_MANAGER_NOT_FOUND");
            }

            return await _vendorDashboardRepo.GetDishDashboardAsync(context.vendorId.Value, context.allowedBranchIds, fromDate, toDate);
        }

        public Task<DishDashboardDto> GetDishDashboardByVendorAsync(int vendorId)
        {
            return _vendorDashboardRepo.GetDishDashboardAsync(vendorId, null, DateTime.MinValue, DateTime.MaxValue);
        }

        public async Task<RevenueBarChartDto> GetRevenueBarChartAsync(int userId, DateTime fromDate, DateTime toDate)
        {
            var context = await _vendorDashboardRepo.GetDashboardContextByUserIdAsync(userId);
            if (context.vendorId == null)
            {
                throw new DomainExceptions("Không tìm thấy Vendor hoặc Manager cho nguời dùng này.", "VENDOR_OR_MANAGER_NOT_FOUND");
            }

            return await _vendorDashboardRepo.GetRevenueBarChartAsync(context.vendorId.Value, context.allowedBranchIds, fromDate, toDate);
        }

        public async Task<BranchesPerformanceDashboardDto> GetBranchesPerformanceAsync(int userId, DateTime fromDate, DateTime toDate)
        {
            var context = await _vendorDashboardRepo.GetDashboardContextByUserIdAsync(userId);
            if (context.vendorId == null)
            {
                throw new DomainExceptions("Không tìm thấy Vendor hoặc Manager cho người dùng này.", "VENDOR_OR_MANAGER_NOT_FOUND");
            }

            return await _vendorDashboardRepo.GetBranchesPerformanceAsync(context.vendorId.Value, context.allowedBranchIds, fromDate, toDate);
        }
    }
}
