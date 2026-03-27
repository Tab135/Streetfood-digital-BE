using BO.Entities;
using BO.Enums;
using DAL;
using Repository.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class UserQuestRepository : IUserQuestRepository
    {
        private readonly UserQuestDAO _dao;

        public UserQuestRepository(UserQuestDAO dao)
        {
            _dao = dao;
        }

        public Task<UserQuest> CreateAsync(UserQuest userQuest) => _dao.CreateAsync(userQuest);
        public Task<UserQuest?> GetByUserAndQuestAsync(int userId, int questId) => _dao.GetByUserAndQuestAsync(userId, questId);
        public Task<UserQuest?> GetByIdAsync(int userQuestId) => _dao.GetByIdAsync(userQuestId);
        public Task<List<UserQuest>> GetByUserIdAsync(int userId, string? status) => _dao.GetByUserIdAsync(userId, status);
        public Task<List<UserQuestTask>> GetInProgressTasksByTypeAsync(int userId, QuestTaskType taskType) => _dao.GetInProgressTasksByTypeAsync(userId, taskType);
        public Task UpdateUserQuestTaskAsync(UserQuestTask userQuestTask) => _dao.UpdateUserQuestTaskAsync(userQuestTask);
        public Task UpdateUserQuestAsync(UserQuest userQuest) => _dao.UpdateUserQuestAsync(userQuest);
        public Task<bool> AreAllTasksCompletedAsync(int userQuestId) => _dao.AreAllTasksCompletedAsync(userQuestId);
        public Task AddUserQuestTasksAsync(List<UserQuestTask> tasks) => _dao.AddUserQuestTasksAsync(tasks);
        public Task<List<UserQuest>> GetByUserAndCampaignAsync(int userId, int campaignId) => _dao.GetByUserAndCampaignAsync(userId, campaignId);
        public Task<List<UserQuest>> GetExpiredQuestsAsync() => _dao.GetExpiredQuestsAsync();
        public Task<UserQuest?> GetActiveStandaloneQuestAsync(int userId) => _dao.GetActiveStandaloneQuestAsync(userId);
        public Task<UserQuest?> GetByUserAndQuestAnyStatusAsync(int userId, int questId) => _dao.GetByUserAndQuestAnyStatusAsync(userId, questId);
    }
}
