using BO.Entities;
using DAL;
using Repository.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class QuestRepository : IQuestRepository
    {
        private readonly QuestDAO _dao;

        public QuestRepository(QuestDAO dao)
        {
            _dao = dao;
        }

        public Task<Quest> CreateAsync(Quest quest) => _dao.CreateAsync(quest);
        public Task<Quest?> GetByIdAsync(int questId) => _dao.GetByIdAsync(questId);
        public Task<(List<Quest> Items, int TotalCount)> GetQuestsAsync(bool? isActive, int? campaignId, int page, int pageSize) => _dao.GetQuestsAsync(isActive, campaignId, page, pageSize);
        public Task<(List<Quest> Items, int TotalCount)> GetPublicQuestsAsync(int? campaignId, int page, int pageSize) => _dao.GetPublicQuestsAsync(campaignId, page, pageSize);
        public Task UpdateAsync(Quest quest) => _dao.UpdateAsync(quest);
        public Task<bool> DeleteAsync(int questId) => _dao.DeleteAsync(questId);
        public Task<bool> HasEnrolledUsersAsync(int questId) => _dao.HasEnrolledUsersAsync(questId);
        public Task<bool> ExistsByCampaignIdAsync(int campaignId, int? excludeQuestId = null) => _dao.ExistsByCampaignIdAsync(campaignId, excludeQuestId);
        public Task<QuestTask?> GetTaskByIdAsync(int questTaskId) => _dao.GetTaskByIdAsync(questTaskId);
        public Task UpdateTaskAsync(QuestTask task) => _dao.UpdateTaskAsync(task);
        public Task RemoveTasksAsync(List<QuestTask> tasks) => _dao.RemoveTasksAsync(tasks);
        public Task AddTasksAsync(List<QuestTask> tasks) => _dao.AddTasksAsync(tasks);
    }
}
