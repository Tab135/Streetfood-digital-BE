using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace Service
{

    public class QuestExpirationJob : IQuestExpirationJob
    {
        private readonly IUserQuestRepository _userQuestRepository;
        private readonly ICampaignRepository _campaignRepository;

        public QuestExpirationJob(
            IUserQuestRepository userQuestRepository,
            ICampaignRepository campaignRepository)
        {
            _userQuestRepository = userQuestRepository;
            _campaignRepository = campaignRepository;
        }

        public async Task ExpireCampaignQuestsAsync(int campaignId)
        {
            var campaign = await _campaignRepository.GetByIdAsync(campaignId);
            if (campaign == null) return;

            // Guard: if EndDate was extended after this job was scheduled,
            // a new job was already created for the real EndDate — abort here.
            if (campaign.EndDate > DateTime.UtcNow) return;

            var userQuests = await _userQuestRepository.GetByUserAndCampaignQuestsInProgressAsync(campaignId);

            foreach (var uq in userQuests)
            {
                uq.Status = "EXPIRED";
                await _userQuestRepository.UpdateUserQuestAsync(uq);
            }
        }
    }
}
