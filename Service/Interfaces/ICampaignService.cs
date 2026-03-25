using BO.DTO.Campaigns;
using BO.Common;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface ICampaignService
    {
        Task CreateSystemCampaignAsync(CreateCampaignDto dto);
        Task CreateRestaurantCampaignAsync(int userId, int branchId, CreateCampaignDto dto);
        Task CreateVendorCampaignAsync(int userId, CreateCampaignDto dto);
        Task<int> JoinSystemCampaignAsync(int userId, int branchId, int campaignId);
        Task<PaginatedResponse<CampaignResponseDto>> GetSystemCampaignsAsync(CampaignQueryDto query);
        Task<PaginatedResponse<CampaignResponseDto>> GetVendorCampaignsAsync(int userId, CampaignQueryDto query);
        Task<CampaignResponseDto> GetCampaignByIdAsync(int id);
        Task UpdateCampaignAsync(int userId, string role, int campaignId, UpdateCampaignDto dto);
    }
}
