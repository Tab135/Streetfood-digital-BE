using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IQuestExpirationJob
    {
        Task ExpireCampaignQuestsAsync(int campaignId);
    }
}
