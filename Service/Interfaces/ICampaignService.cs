using BO.DTO.Campaigns;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface ICampaignService
    {
        Task CreateSystemCampaignAsync(CreateCampaignDto dto);
        Task CreateRestaurantCampaignAsync(int userId, int branchId, CreateCampaignDto dto);
        Task<int> JoinSystemCampaignAsync(int userId, int branchId, int campaignId);
    }
}
