using BO.Entities;
using BO.Enums;
using BO.DTO.Quest;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class UserQuestDAO
    {
        private readonly StreetFoodDbContext _context;

        public UserQuestDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<UserQuest> CreateAsync(UserQuest userQuest)
        {
            _context.UserQuests.Add(userQuest);
            await _context.SaveChangesAsync();
            return userQuest;
        }

        public async Task<UserQuest?> GetByUserAndQuestAsync(int userId, int questId)
        {
            return await _context.UserQuests
                .Include(uq => uq.UserQuestTasks)
                    .ThenInclude(uqt => uqt.QuestTask)
                .Include(uq => uq.Quest)
                .FirstOrDefaultAsync(uq => uq.UserId == userId && uq.QuestId == questId);
        }

        public async Task<UserQuest?> GetByIdAsync(int userQuestId)
        {
            return await _context.UserQuests
                .Include(uq => uq.UserQuestTasks)
                    .ThenInclude(uqt => uqt.QuestTask)
                .Include(uq => uq.Quest)
                .FirstOrDefaultAsync(uq => uq.UserQuestId == userQuestId);
        }

        public async Task<(List<UserQuest> Items, int TotalCount)> GetByUserIdAsync(int userId, string? status, bool? isTierUp = null, int page = 1, int pageSize = 10)
        {
            var query = _context.UserQuests
                .Include(uq => uq.UserQuestTasks)
                    .ThenInclude(uqt => uqt.QuestTask)
                .Include(uq => uq.Quest)
                    .ThenInclude(q => q.QuestTasks)
                .Where(uq => uq.UserId == userId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(uq => uq.Status == status);

            if (isTierUp == true)
                query = query.Where(uq => uq.Quest.QuestTasks.Any(t => t.Type == QuestTaskType.TIER_UP));
            else if (isTierUp == false)
                query = query.Where(uq => !uq.Quest.QuestTasks.Any(t => t.Type == QuestTaskType.TIER_UP));

            int totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(uq => uq.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<UserQuestTask>> GetInProgressTasksByTypeAsync(int userId, QuestTaskType taskType)
        {
            return await _context.UserQuestTasks
                .Include(uqt => uqt.QuestTask)
                .Include(uqt => uqt.UserQuest)
                .Where(uqt => uqt.UserQuest.UserId == userId
                    && uqt.UserQuest.Status == "IN_PROGRESS"
                    && !uqt.IsCompleted
                    && uqt.QuestTask.Type == taskType)
                .ToListAsync();
        }

        public async Task UpdateUserQuestTaskAsync(UserQuestTask userQuestTask)
        {
            _context.UserQuestTasks.Update(userQuestTask);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserQuestAsync(UserQuest userQuest)
        {
            _context.UserQuests.Update(userQuest);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AreAllTasksCompletedAsync(int userQuestId)
        {
            return await _context.UserQuestTasks
                .Where(uqt => uqt.UserQuestId == userQuestId)
                .AllAsync(uqt => uqt.IsCompleted);
        }

        public async Task AddUserQuestTasksAsync(List<UserQuestTask> tasks)
        {
            _context.UserQuestTasks.AddRange(tasks);
            await _context.SaveChangesAsync();
        }

        public async Task<List<UserQuest>> GetByUserAndCampaignAsync(int userId, int campaignId)
        {
            return await _context.UserQuests
                .Include(uq => uq.UserQuestTasks)
                    .ThenInclude(uqt => uqt.QuestTask)
                .Include(uq => uq.Quest)
                    .ThenInclude(q => q.QuestTasks)
                .Where(uq => uq.UserId == userId && uq.Quest.CampaignId == campaignId)
                .OrderByDescending(uq => uq.StartedAt)
                .ToListAsync();
        }

        public async Task<(List<UserQuest> Items, int TotalCount)> GetUserQuestTasksByQuestAsync(UserQuestTaskQueryDto query)
        {
            var normalizedPage = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var normalizedPageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            var dbQuery = _context.UserQuests
                .Include(uq => uq.User).ThenInclude(u => u.Tier)
                .Include(uq => uq.Quest)
                .Include(uq => uq.UserQuestTasks)
                    .ThenInclude(t => t.QuestTask)
                        .ThenInclude(qt => qt.QuestTaskRewards)
                .AsQueryable();

            if (query.QuestId.HasValue)
                dbQuery = dbQuery.Where(uq => uq.QuestId == query.QuestId.Value);   

            if (query.UserId.HasValue)
                dbQuery = dbQuery.Where(uq => uq.UserId == query.UserId.Value);

            if (query.UserQuestId.HasValue)
                dbQuery = dbQuery.Where(uq => uq.UserQuestId == query.UserQuestId.Value);

            if (!string.IsNullOrWhiteSpace(query.Status))
                dbQuery = dbQuery.Where(uq => uq.Status == query.Status);

            if (query.QuestTaskId.HasValue)
                dbQuery = dbQuery.Where(uq => uq.UserQuestTasks.Any(t => t.QuestTaskId == query.QuestTaskId.Value));

            if (query.Type.HasValue)
                dbQuery = dbQuery.Where(uq => uq.UserQuestTasks.Any(t => t.QuestTask.Type == query.Type.Value));

            if (query.IsCompleted.HasValue)
                dbQuery = dbQuery.Where(uq => uq.UserQuestTasks.Any(t => t.IsCompleted == query.IsCompleted.Value));

            if (query.RewardClaimed.HasValue)
                dbQuery = dbQuery.Where(uq => uq.UserQuestTasks.Any(t => t.RewardClaimed == query.RewardClaimed.Value));

            if (query.CompletedFrom.HasValue)
                dbQuery = dbQuery.Where(uq => uq.UserQuestTasks.Any(t => t.CompletedAt.HasValue && t.CompletedAt.Value >= query.CompletedFrom.Value));

            if (query.CompletedTo.HasValue)
                dbQuery = dbQuery.Where(uq => uq.UserQuestTasks.Any(t => t.CompletedAt.HasValue && t.CompletedAt.Value <= query.CompletedTo.Value));

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var keyword = query.Search.Trim().ToLower();
                dbQuery = dbQuery.Where(uq =>
                    (uq.User.UserName != null && uq.User.UserName.ToLower().Contains(keyword)) ||
                    (uq.User.Email != null && uq.User.Email.ToLower().Contains(keyword)) ||
                    (uq.User.FirstName != null && uq.User.FirstName.ToLower().Contains(keyword)) ||
                    (uq.User.LastName != null && uq.User.LastName.ToLower().Contains(keyword)));
            }

            var totalCount = await dbQuery.CountAsync();
            var items = await dbQuery
                .OrderByDescending(uq => uq.StartedAt)
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<UserQuest>> GetExpiredQuestsAsync()
        {
            return await _context.UserQuests
                .Include(uq => uq.Quest)
                    .ThenInclude(q => q.Campaign)
                .Where(uq => uq.Status == "IN_PROGRESS"
                    && uq.Quest.CampaignId != null
                    && uq.Quest.Campaign!.EndDate < DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<UserQuest?> GetActiveStandaloneQuestAsync(int userId)
        {
            return await _context.UserQuests
                .Include(uq => uq.Quest)
                .Where(uq => uq.UserId == userId
                    && uq.Status == "IN_PROGRESS"
                    && uq.Quest.IsStandalone)
                .FirstOrDefaultAsync();
        }

        public async Task<UserQuest?> GetByUserAndQuestAnyStatusAsync(int userId, int questId)
        {
            return await _context.UserQuests
                .Include(uq => uq.UserQuestTasks)
                    .ThenInclude(uqt => uqt.QuestTask)
                .Include(uq => uq.Quest)
                .FirstOrDefaultAsync(uq => uq.UserId == userId && uq.QuestId == questId);
        }

        public async Task<List<UserQuest>> GetByUserAndCampaignQuestsInProgressAsync(int campaignId)
        {
            return await _context.UserQuests
                .Include(uq => uq.Quest)
                .Where(uq => uq.Status == "IN_PROGRESS" && uq.Quest.CampaignId == campaignId)
                .ToListAsync();
        }
    }
}
