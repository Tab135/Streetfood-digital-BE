using BO.Entities;
using BO.Enums;
using BO.DTO.Quest;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IUserQuestRepository
    {
        Task<UserQuest> CreateAsync(UserQuest userQuest);
        Task<UserQuest?> GetByUserAndQuestAsync(int userId, int questId);
        Task<UserQuest?> GetByIdAsync(int userQuestId);
        Task<(List<UserQuest> Items, int TotalCount)> GetByUserIdAsync(int userId, string? status, bool? isTierUp = null, int page = 1, int pageSize = 10);
        Task<List<UserQuestTask>> GetInProgressTasksByTypeAsync(int userId, QuestTaskType taskType);
        Task UpdateUserQuestTaskAsync(UserQuestTask userQuestTask);
        Task UpdateUserQuestAsync(UserQuest userQuest);
        Task<bool> AreAllTasksCompletedAsync(int userQuestId);
        Task AddUserQuestTasksAsync(List<UserQuestTask> tasks);
        Task<List<UserQuest>> GetByUserAndCampaignAsync(int userId, int campaignId);
        Task<(List<UserQuest> Items, int TotalCount)> GetUserQuestTasksByQuestAsync(UserQuestTaskQueryDto query);
        /// <summary>Returns the active (IN_PROGRESS) UserQuest for this user — campaign or standalone — or null. Used to enforce the one-active-quest-at-a-time rule.</summary>
        Task<UserQuest?> GetActiveUserQuestAsync(int userId);
        /// <summary>Returns the UserQuest for (userId, questId) regardless of status, or null.</summary>
        Task<UserQuest?> GetByUserAndQuestAnyStatusAsync(int userId, int questId);
        /// <summary>Returns all UserQuests in IN_PROGRESS or STOPPED state whose Quest belongs to the given campaign. Used by the expiration job so paused quests still expire on campaign EndDate.</summary>
        Task<List<UserQuest>> GetActiveOrStoppedByCampaignAsync(int campaignId);
    }
}
