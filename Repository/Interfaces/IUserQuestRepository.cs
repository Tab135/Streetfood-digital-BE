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
        Task<(List<UserQuest> Items, int TotalCount)> GetUserQuestsAsync(UserQuestQueryDto query);
        Task<(List<UserQuestTask> Items, int TotalCount)> GetUserQuestTasksByQuestAsync(int questId, UserQuestTaskQueryDto query);
        /// <summary>Returns the active (IN_PROGRESS) standalone UserQuest for this user, or null.</summary>
        Task<UserQuest?> GetActiveStandaloneQuestAsync(int userId);
        /// <summary>Returns the UserQuest for (userId, questId) regardless of status, or null.</summary>
        Task<UserQuest?> GetByUserAndQuestAnyStatusAsync(int userId, int questId);
        /// <summary>Returns all IN_PROGRESS UserQuests whose Quest belongs to the given campaign.</summary>
        Task<List<UserQuest>> GetByUserAndCampaignQuestsInProgressAsync(int campaignId);
    }
}
