using BO.Entities;
using BO.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IUserQuestRepository
    {
        Task<UserQuest> CreateAsync(UserQuest userQuest);
        Task<UserQuest?> GetByUserAndQuestAsync(int userId, int questId);
        Task<UserQuest?> GetByIdAsync(int userQuestId);
        Task<List<UserQuest>> GetByUserIdAsync(int userId, string? status);
        Task<List<UserQuestTask>> GetInProgressTasksByTypeAsync(int userId, QuestTaskType taskType);
        Task UpdateUserQuestTaskAsync(UserQuestTask userQuestTask);
        Task UpdateUserQuestAsync(UserQuest userQuest);
        Task<bool> AreAllTasksCompletedAsync(int userQuestId);
        Task AddUserQuestTasksAsync(List<UserQuestTask> tasks);
        Task<List<UserQuest>> GetByUserAndCampaignAsync(int userId, int campaignId);
        /// <summary>Returns the active (IN_PROGRESS) standalone UserQuest for this user, or null.</summary>
        Task<UserQuest?> GetActiveStandaloneQuestAsync(int userId);
        /// <summary>Returns the UserQuest for (userId, questId) regardless of status, or null.</summary>
        Task<UserQuest?> GetByUserAndQuestAnyStatusAsync(int userId, int questId);
        /// <summary>Returns all IN_PROGRESS UserQuests whose Quest belongs to the given campaign.</summary>
        Task<List<UserQuest>> GetByUserAndCampaignQuestsInProgressAsync(int campaignId);
    }
}
