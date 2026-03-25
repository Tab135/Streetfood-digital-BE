using BO.DTO.Campaigns;
using BO.Common;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface ICampaignService
    {
        Task<CampaignResponseDto> CreateSystemCampaignAsync(CreateCampaignDto dto);
        Task<CampaignResponseDto> CreateRestaurantCampaignAsync(int userId, int branchId, CreateCampaignDto dto);
        Task<CampaignResponseDto> CreateVendorCampaignAsync(int userId, CreateCampaignDto dto);
        Task<int> JoinSystemCampaignAsync(int userId, int branchId, int campaignId);
        Task<PaginatedResponse<CampaignResponseDto>> GetSystemCampaignsAsync(CampaignQueryDto query);
        Task<PaginatedResponse<CampaignResponseDto>> GetVendorCampaignsAsync(int userId, CampaignQueryDto query);
        Task<CampaignResponseDto> GetCampaignByIdAsync(int id);
        Task<CampaignResponseDto> UpdateCampaignAsync(int userId, string role, int campaignId, UpdateCampaignDto dto);
    }
}
