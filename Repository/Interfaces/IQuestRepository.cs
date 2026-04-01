using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IQuestRepository
    {
        Task<Quest> CreateAsync(Quest quest);
        Task<Quest?> GetByIdAsync(int questId);
        Task<(List<Quest> Items, int TotalCount)> GetQuestsAsync(bool? isActive, int? campaignId, int page, int pageSize);
        Task<(List<Quest> Items, int TotalCount)> GetPublicQuestsAsync(int? campaignId, int page, int pageSize);
        Task UpdateAsync(Quest quest);
        Task<bool> DeleteAsync(int questId);
        Task<bool> HasEnrolledUsersAsync(int questId);
        Task<bool> ExistsByCampaignIdAsync(int campaignId, int? excludeQuestId = null);
        Task UpdateTaskAsync(QuestTask task);
    Task RemoveTasksAsync(List<QuestTask> tasks);
        Task AddTasksAsync(List<QuestTask> tasks);
    }
}
