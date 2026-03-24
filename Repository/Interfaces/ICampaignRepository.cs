using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface ICampaignRepository
    {
        Task<Campaign> CreateAsync(Campaign campaign);
        Task<Campaign?> GetByIdAsync(int id);
        Task<List<Campaign>> GetAllSystemActiveAsync();
        Task<List<Campaign>> GetByBranchIdAsync(int branchId);
        Task UpdateAsync(Campaign campaign);
    }
}
