using BO.DTO.Dashboard;
using BO.Exceptions;
using DAL;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace Service
{
    public class VendorDashboardService : IVendorDashboardService
    {
        private readonly VendorDashboardDAO _vendorDashboardDao;

        public VendorDashboardService(VendorDashboardDAO vendorDashboardDao)
        {
            _vendorDashboardDao = vendorDashboardDao ?? throw new ArgumentNullException(nameof(vendorDashboardDao));
        }

        public async Task<RevenueDashboardDto> GetRevenueDashboardAsync(int userId, DateTime fromDate, DateTime toDate)
        {
            var vendorId = await _vendorDashboardDao.GetVendorIdByUserIdAsync(userId);
            if (vendorId == null)
            {
                throw new DomainExceptions("Vendor not found for this user.", "VENDOR_NOT_FOUND");
            }

            return await _vendorDashboardDao.GetRevenueDashboardAsync(vendorId.Value, fromDate, toDate);
        }

        public async Task<VoucherDashboardDto> GetVoucherDashboardAsync(int userId)
        {
            var vendorId = await _vendorDashboardDao.GetVendorIdByUserIdAsync(userId);
            if (vendorId == null)
            {
                throw new DomainExceptions("Vendor not found for this user.", "VENDOR_NOT_FOUND");
            }

            return await _vendorDashboardDao.GetVoucherDashboardAsync(vendorId.Value);
        }

        public async Task<DishDashboardDto> GetDishDashboardAsync(int userId)
        {
            var vendorId = await _vendorDashboardDao.GetVendorIdByUserIdAsync(userId);
            if (vendorId == null)
            {
                throw new DomainExceptions("Vendor not found for this user.", "VENDOR_NOT_FOUND");
            }

            return await _vendorDashboardDao.GetDishDashboardAsync(vendorId.Value);
        }
    }
}
