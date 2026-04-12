using BO.DTO.Campaigns;
using BO.Entities;
using DAL;
using Repository.Interfaces;
using System;
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
        public Task DeleteAsync(int campaignId) => _dao.DeleteAsync(campaignId);
        public Task<List<int>> GetCampaignIdsToActivateAsync(DateTime now) => _dao.GetCampaignIdsToActivateAsync(now);
        public Task<List<int>> GetExpiredCampaignIdsAsync(DateTime now) => _dao.GetExpiredCampaignIdsAsync(now);
        public Task<List<int>> GetCampaignIdsToOpenRegistrationAsync(DateTime now) => _dao.GetCampaignIdsToOpenRegistrationAsync(now);
        public Task<List<int>> GetCampaignIdsToCloseRegistrationAsync(DateTime now) => _dao.GetCampaignIdsToCloseRegistrationAsync(now);
        public Task<(List<Campaign> Items, int TotalCount)> GetCampaignsAsync(bool? isSystem, int? vendorId, int page, int pageSize) => _dao.GetCampaignsAsync(isSystem, vendorId, page, pageSize);
        public Task<(List<Campaign> Items, int TotalCount)> GetJoinableSystemCampaignsAsync(int page, int pageSize) => _dao.GetJoinableSystemCampaignsAsync(page, pageSize);
        public Task<(List<Campaign> Items, int TotalCount)> GetPublicCampaignsAsync(bool? isSystem, int page, int pageSize) => _dao.GetPublicCampaignsAsync(isSystem, page, pageSize);
        public Task<(List<CampaignBranchResponseDto> Items, int TotalCount)> GetBranchesInAnyVendorCampaignPaginatedAsync(int pageNumber, int pageSize, double? userLat, double? userLng, double? maxDistance = 5.0) => _dao.GetBranchesInAnyVendorCampaignPaginatedAsync(pageNumber, pageSize, userLat, userLng, maxDistance);
        public Task<(List<CampaignBranchResponseDto> Items, int TotalCount)> GetCampaignBranchesPaginatedAsync(int campaignId, int pageNumber, int pageSize, double? userLat, double? userLng, bool includeInactiveBranches = false) => _dao.GetCampaignBranchesPaginatedAsync(campaignId, pageNumber, pageSize, userLat, userLng, includeInactiveBranches);

    }
}
