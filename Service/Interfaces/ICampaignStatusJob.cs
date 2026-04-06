using System.Threading.Tasks;

namespace Service.Interfaces;

public interface ICampaignStatusJob
{
    Task ActivateCampaignAsync(int campaignId);
    Task DeactivateCampaignAsync(int campaignId);
    Task ReconcileCampaignStatusesAsync();
}
