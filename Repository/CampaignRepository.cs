using BO.Entities;
using DAL;
using Repository.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class CampaignRepository : ICampaignRepository
    {
        private readonly CampaignDAO _dao;

        public CampaignRepository(CampaignDAO dao)
        {
            _dao = dao;
        }

        public Task<Campaign> CreateAsync(Campaign campaign) => _dao.CreateAsync(campaign);
        public Task<Campaign?> GetByIdAsync(int id) => _dao.GetByIdAsync(id);
        public Task<List<Campaign>> GetAllSystemActiveAsync() => _dao.GetAllSystemActiveAsync();
        public Task<List<Campaign>> GetByBranchIdAsync(int branchId) => _dao.GetByBranchIdAsync(branchId);
        public Task UpdateAsync(Campaign campaign) => _dao.UpdateAsync(campaign);
    }
}
