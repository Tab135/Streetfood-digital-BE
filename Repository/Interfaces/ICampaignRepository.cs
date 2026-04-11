using BO.DTO.Campaigns;
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
        Task<(List<Campaign> Items, int TotalCount)> GetCampaignsAsync(bool? isSystem, int? vendorId, int page, int pageSize);
        Task<(List<Campaign> Items, int TotalCount)> GetJoinableSystemCampaignsAsync(int page, int pageSize);
        Task<(List<Campaign> Items, int TotalCount)> GetPublicCampaignsAsync(bool? isSystem, int page, int pageSize);
        Task<(List<CampaignBranchResponseDto> Items, int TotalCount)> GetBranchesInAnyVendorCampaignPaginatedAsync(int pageNumber, int pageSize, double? userLat, double? userLng, double? maxDistance = 5.0);
        Task<(List<CampaignBranchResponseDto> Items, int TotalCount)> GetCampaignBranchesPaginatedAsync(int campaignId, int pageNumber, int pageSize, double? userLat, double? userLng, bool includeInactiveBranches = false);

        // Images
    }
}