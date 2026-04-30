using BO.DTO.Campaigns;
using BO.Entities;
using System;
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
        Task DeleteAsync(int campaignId);
        Task<List<int>> GetCampaignIdsToActivateAsync(DateTime now);
        Task<List<int>> GetExpiredCampaignIdsAsync(DateTime now);
        Task<List<int>> GetCampaignIdsToOpenRegistrationAsync(DateTime now);
        Task<List<int>> GetCampaignIdsToCloseRegistrationAsync(DateTime now);
        Task<(List<Campaign> Items, int TotalCount)> GetCampaignsAsync(bool? isSystem, int? vendorId, int page, int pageSize);
        Task<(List<Campaign> Items, int TotalCount)> GetJoinableSystemCampaignsAsync(int page, int pageSize);
        Task<(List<Campaign> Items, int TotalCount)> GetPublicCampaignsAsync(bool? isSystem, int page, int pageSize);
        Task<(List<CampaignBranchResponseDto> Items, int TotalCount)> GetBranchesInAnyVendorCampaignPaginatedAsync(int pageNumber, int pageSize, double? userLat, double? userLng, double? maxDistance = 5.0);
        Task<(List<CampaignBranchResponseDto> Items, int TotalCount)> GetCampaignBranchesPaginatedAsync(int campaignId, int pageNumber, int pageSize, double? userLat, double? userLng, bool includeInactiveBranches = false);
        Task<List<BranchCampaignInfoDto>> GetVendorCampaignsByBranchAsync(int branchId, bool? isWorking = null);

        // Images
    }
}