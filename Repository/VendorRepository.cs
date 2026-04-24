using BO.Entities;
using DAL;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class VendorRepository : IVendorRepository
    {
        private readonly VendorDAO _vendorDAO;

        public VendorRepository(VendorDAO vendorDAO)
        {
            _vendorDAO = vendorDAO ?? throw new ArgumentNullException(nameof(vendorDAO));
        }

        public async Task<Vendor> CreateAsync(Vendor vendor)
        {
            return await _vendorDAO.CreateAsync(vendor);
        }

        public async Task<Vendor> GetByIdAsync(int vendorId)
        {
            return await _vendorDAO.GetByIdAsync(vendorId);
        }

        public async Task<Vendor> GetByUserIdAsync(int userId)
        {
            return await _vendorDAO.GetByUserIdAsync(userId);
        }

        public async Task<(List<Vendor> items, int totalCount)> GetAllAsync(int pageNumber, int pageSize)
        {
            return await _vendorDAO.GetAllAsync(pageNumber, pageSize);
        }

        public async Task<(List<Vendor> items, int totalCount)> GetActiveVendorsAsync(int pageNumber, int pageSize)
        {
            return await _vendorDAO.GetActiveVendorsAsync(pageNumber, pageSize);
        }

        public async Task UpdateAsync(Vendor vendor)
        {
            await _vendorDAO.UpdateAsync(vendor);
        }

        public async Task DeleteAsync(int vendorId)
        {
            await _vendorDAO.DeleteAsync(vendorId);
        }

        public async Task<bool> ExistsByIdAsync(int vendorId)
        {
            return await _vendorDAO.ExistsByIdAsync(vendorId);
        }

        public async Task<bool> ExistsByUserIdAsync(int userId)
        {
            return await _vendorDAO.ExistsByUserIdAsync(userId);
        }

        public async Task RefundCampaignJoinFeeAsync(int campaignId, decimal fee)
        {
            await _vendorDAO.RefundCampaignJoinFeeAsync(campaignId, fee);
        }
    }
}
