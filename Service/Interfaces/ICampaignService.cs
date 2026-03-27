using BO.DTO.Campaigns;
using BO.Common;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface ICampaignService
    {
        Task<CampaignResponseDto> CreateSystemCampaignAsync(CreateCampaignDto dto);
        Task<CampaignResponseDto> CreateRestaurantCampaignAsync(int userId, int branchId, CreateVendorCampaignDto dto);
        Task<CampaignResponseDto> CreateVendorCampaignAsync(int userId, CreateVendorCampaignDto dto);
        Task<int> JoinSystemCampaignAsync(int userId, int branchId, int campaignId);
        Task<PaginatedResponse<CampaignResponseDto>> GetSystemCampaignsAsync(CampaignQueryDto query);
        Task<PaginatedResponse<CampaignResponseDto>> GetVendorCampaignsAsync(int userId, CampaignQueryDto query);
        Task<PaginatedResponse<CampaignResponseDto>> GetJoinableSystemCampaignsAsync(CampaignQueryDto query);
        Task<PaginatedResponse<CampaignResponseDto>> GetPublicCampaignsAsync(CampaignQueryDto query);
        Task<PaginatedResponse<CampaignResponseDto>> GetCampaignsByBranchAsync(int userId, string role, int branchId, CampaignQueryDto query);
        
        Task<CampaignResponseDto> GetCampaignByIdAsync(int id);
        Task<CampaignResponseDto> UpdateCampaignAsync(int userId, string role, int campaignId, UpdateCampaignDto dto);

        // Images
        Task UpdateCampaignImageUrlAsync(int campaignId, string? imageUrl, int userId, string role);
        Task<string?> GetCampaignImageUrlAsync(int campaignId);
        // New: Get system campaign detail with eligible branches
        Task<SystemCampaignDetailDto> GetSystemCampaignDetailWithJoinableBranchesAsync(int userId, int campaignId);

        // New: Vendor join system campaign for all eligible branches
        Task<VendorJoinSystemCampaignResultDto> VendorJoinSystemCampaignAsync(int userId, int campaignId);

        // New: Vendor join system campaign for selected branches (batch payment)
        Task<VendorJoinSystemCampaignResultDto> VendorJoinSystemCampaignForBranchesAsync(int userId, int campaignId, System.Collections.Generic.List<int> branchIds);
    }
}
