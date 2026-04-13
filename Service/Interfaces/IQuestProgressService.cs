using BO.Enums;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IQuestProgressService
    {
        Task UpdateProgressAsync(int userId, QuestTaskType taskType, int incrementValue);

        /// <summary>
        /// Called when a user's tier changes to <paramref name="newTierId"/>.
        /// Auto-completes the matching active TIER_UP quest (if any) and distributes the reward.
        /// No-op for Warning (1) and Silver (2).
        /// </summary>
        Task HandleTierUpAsync(int userId, int newTierId);
    }
}
