using BO.Entities;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IBranchCampaignRepository
    {
        Task<BranchCampaign> CreateAsync(BranchCampaign branchCampaign);
        Task<BranchCampaign?> GetByIdAsync(int id);
        Task<BranchCampaign?> GetByBranchAndCampaignAsync(int branchId, int campaignId);
        Task UpdateAsync(BranchCampaign branchCampaign);
    }
}