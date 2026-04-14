using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class QuestDAO
    {
        private readonly StreetFoodDbContext _context;

        public QuestDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Quest> CreateAsync(Quest quest)
        {
            _context.Quests.Add(quest);
            await _context.SaveChangesAsync();
            return quest;
        }

        public async Task<Quest?> GetByIdAsync(int questId)
        {
            return await _context.Quests
                .Include(q => q.QuestTasks)
                .FirstOrDefaultAsync(q => q.QuestId == questId);
        }

        public async Task<(List<Quest> Items, int TotalCount)> GetQuestsAsync(
            bool? isActive, int? campaignId, int page, int pageSize)
        {
            var query = _context.Quests.Include(q => q.QuestTasks).AsQueryable();

            if (isActive.HasValue)
                query = query.Where(q => q.IsActive == isActive.Value);

            if (campaignId.HasValue)
                query = query.Where(q => q.CampaignId == campaignId.Value);

            int totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(q => q.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<Quest> Items, int TotalCount)> GetPublicQuestsAsync(int? campaignId, bool? isStandalone, bool? isTierUp, int page, int pageSize)
        {
            var query = _context.Quests
                .Include(q => q.QuestTasks)
                .Where(q => q.IsActive);

            if (isTierUp == true)
                query = query.Where(q => q.QuestTasks.Any(t => t.Type == BO.Enums.QuestTaskType.TIER_UP));
            else
                query = query.Where(q => !q.QuestTasks.Any(t => t.Type == BO.Enums.QuestTaskType.TIER_UP));

            if (campaignId.HasValue)
                query = query.Where(q => q.CampaignId == campaignId.Value);

            if (isStandalone.HasValue)
                query = query.Where(q => q.IsStandalone == isStandalone.Value);

            int totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(q => q.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }


        public async Task UpdateAsync(Quest quest)
        {
            quest.UpdatedAt = DateTime.UtcNow;
            _context.Quests.Update(quest);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int questId)
        {
            var quest = await _context.Quests
                .Include(q => q.QuestTasks)
                .FirstOrDefaultAsync(q => q.QuestId == questId);
            if (quest == null) return false;

            _context.Quests.Remove(quest);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasEnrolledUsersAsync(int questId)
        {
            return await _context.UserQuests.AnyAsync(uq => uq.QuestId == questId);
        }

        public async Task<bool> ExistsByCampaignIdAsync(int campaignId, int? excludeQuestId = null)
        {
            return await _context.Quests.AnyAsync(q =>
                q.CampaignId == campaignId &&
                (!excludeQuestId.HasValue || q.QuestId != excludeQuestId.Value));
        }

        public async Task<bool> HasActiveTierUpQuestForTierAsync(int tierId, int? excludeQuestId = null)
        {
            return await _context.Quests.AnyAsync(q =>
                q.IsActive &&
                !q.RequiresEnrollment &&
                q.QuestTasks.Any(t => t.Type == BO.Enums.QuestTaskType.TIER_UP && t.TargetValue == tierId) &&
                (!excludeQuestId.HasValue || q.QuestId != excludeQuestId.Value));
        }

        public async Task<Quest?> GetActiveTierUpQuestForTierAsync(int tierId)
        {
            return await _context.Quests
                .Include(q => q.QuestTasks)
                    .ThenInclude(t => t.QuestTaskRewards)
                .FirstOrDefaultAsync(q =>
                    q.IsActive &&
                    !q.RequiresEnrollment &&
                    q.QuestTasks.Any(t => t.Type == BO.Enums.QuestTaskType.TIER_UP && t.TargetValue == tierId));
        }

        public async Task<QuestTask?> GetTaskByIdAsync(int questTaskId)
        {
            return await _context.QuestTasks
                .Include(t => t.QuestTaskRewards)
                .FirstOrDefaultAsync(t => t.QuestTaskId == questTaskId);
        }

        public async Task UpdateTaskAsync(QuestTask task)
        {
            _context.QuestTasks.Update(task);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveTasksAsync(List<QuestTask> tasks)
        {
            _context.QuestTasks.RemoveRange(tasks);
            await _context.SaveChangesAsync();
        }

        public async Task AddTasksAsync(List<QuestTask> tasks)
        {
            _context.QuestTasks.AddRange(tasks);
            await _context.SaveChangesAsync();
        }
    }
}
