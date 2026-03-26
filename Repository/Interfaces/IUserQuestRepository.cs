using BO.Entities;
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
        Task<List<UserQuestTask>> GetInProgressTasksByTypeAsync(int userId, string taskType);
        Task UpdateUserQuestTaskAsync(UserQuestTask userQuestTask);
        Task UpdateUserQuestAsync(UserQuest userQuest);
        Task<bool> AreAllTasksCompletedAsync(int userQuestId);
        Task AddUserQuestTasksAsync(List<UserQuestTask> tasks);
        Task<List<UserQuest>> GetByUserAndCampaignAsync(int userId, int campaignId);
        Task<List<UserQuest>> GetExpiredQuestsAsync();
    }
}
