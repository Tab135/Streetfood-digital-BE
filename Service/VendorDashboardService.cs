using BO.DTO.Dashboard;
using BO.Exceptions;
using DAL;
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
            var vendorId = await _vendorDashboardRepo.GetVendorIdByUserIdAsync(userId);
            if (vendorId == null)
            {
                throw new DomainExceptions("Không tìm thấy Vendor cho người dùng này.", "VENDOR_NOT_FOUND");
            }

            return await _vendorDashboardRepo.GetRevenueDashboardAsync(vendorId.Value, fromDate, toDate);
        }

        public async Task<CampaignDashboardDto> GetCampaignDashboardAsync(int userId, DateTime fromDate, DateTime toDate)
        {
            var vendorId = await _vendorDashboardRepo.GetVendorIdByUserIdAsync(userId);
            if (vendorId == null)
            {
                throw new DomainExceptions("Không tìm thấy Vendor cho người dùng này.", "VENDOR_NOT_FOUND");
            }

            return await _vendorDashboardRepo.GetCampaignDashboardAsync(vendorId.Value, fromDate, toDate);
        }

        public async Task<VoucherDashboardDto> GetVoucherDashboardAsync(int userId)
        {
            var vendorId = await _vendorDashboardRepo.GetVendorIdByUserIdAsync(userId);
            if (vendorId == null)
            {
                throw new DomainExceptions("Không tìm thấy Vendor cho người dùng này.", "VENDOR_NOT_FOUND");
            }

            return await _vendorDashboardRepo.GetVoucherDashboardAsync(vendorId.Value);
        }

        public async Task<DishDashboardDto> GetDishDashboardAsync(int userId)
        {
            var vendorId = await _vendorDashboardRepo.GetVendorIdByUserIdAsync(userId);
            if (vendorId == null)
            {
                throw new DomainExceptions("Không tìm thấy Vendor cho người dùng này.", "VENDOR_NOT_FOUND");
            }

            return await _vendorDashboardRepo.GetDishDashboardAsync(vendorId.Value);
        }

        public Task<DishDashboardDto> GetDishDashboardByVendorAsync(int vendorId)
        {
            return _vendorDashboardRepo.GetDishDashboardAsync(vendorId);
        }
    }
}
