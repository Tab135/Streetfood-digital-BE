using BO.Entities;
using DAL;
using Repository.Interfaces;
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
        public Task UpdateAsync(BranchCampaign branchCampaign) => _dao.UpdateAsync(branchCampaign);
    }
}