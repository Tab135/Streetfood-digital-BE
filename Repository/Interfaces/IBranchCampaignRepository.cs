using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IBranchCampaignRepository
    {
        Task<BranchCampaign> CreateAsync(BranchCampaign branchCampaign);
        Task<BranchCampaign?> GetByIdAsync(int id);
        Task<BranchCampaign?> GetByBranchAndCampaignAsync(int branchId, int campaignId);
        // For batch vendor payment: fetch all not-yet-paid branch-campaign rows of the vendor
        Task<List<BranchCampaign>> GetPendingByCampaignAndVendorAsync(int campaignId, int vendorId);
        Task UpdateAsync(BranchCampaign branchCampaign);
    }
}