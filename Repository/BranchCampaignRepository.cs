using BO.Entities;
using DAL;
using Repository.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class BranchCampaignRepository : IBranchCampaignRepository
    {
        private readonly BranchCampaignDAO _dao;

        public BranchCampaignRepository(BranchCampaignDAO dao)
        {
            _dao = dao;
        }

        public Task<BranchCampaign> CreateAsync(BranchCampaign branchCampaign) => _dao.CreateAsync(branchCampaign);
        public Task<BranchCampaign?> GetByIdAsync(int id) => _dao.GetByIdAsync(id);
        public Task<BranchCampaign?> GetByBranchAndCampaignAsync(int branchId, int campaignId) => _dao.GetByBranchAndCampaignAsync(branchId, campaignId);
        public Task<List<BranchCampaign>> GetPendingByCampaignAndVendorAsync(int campaignId, int vendorId) => _dao.GetPendingByCampaignAndVendorAsync(campaignId, vendorId);
        public Task UpdateAsync(BranchCampaign branchCampaign) => _dao.UpdateAsync(branchCampaign);
        public Task<bool> DeleteByBranchAndCampaignAsync(int branchId, int campaignId) => _dao.DeleteByBranchAndCampaignAsync(branchId, campaignId);
        public Task<List<int>> GetBranchIdsByCampaignAndVendorAsync(int campaignId, int vendorId) => _dao.GetBranchIdsByCampaignAndVendorAsync(campaignId, vendorId);
        public Task<int> CountByCampaignIdAsync(int campaignId) => _dao.CountByCampaignIdAsync(campaignId);
        public Task SetAllIsActiveForCampaignAsync(int campaignId, bool isActive) => _dao.SetAllIsActiveForCampaignAsync(campaignId, isActive);
    }
}