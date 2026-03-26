using BO.Entities;
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

        public async Task<List<UserQuest>> GetByUserIdAsync(int userId, string? status)
        {
            var query = _context.UserQuests
                .Include(uq => uq.UserQuestTasks)
                    .ThenInclude(uqt => uqt.QuestTask)
                .Include(uq => uq.Quest)
                    .ThenInclude(q => q.QuestTasks)
                .Where(uq => uq.UserId == userId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(uq => uq.Status == status);

            return await query.OrderByDescending(uq => uq.StartedAt).ToListAsync();
        }

        public async Task<List<UserQuestTask>> GetInProgressTasksByTypeAsync(int userId, string taskType)
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

        public async Task<List<UserQuest>> GetExpiredQuestsAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.UserQuests
                .Include(uq => uq.Quest)
                .Where(uq => uq.Status == "IN_PROGRESS" && uq.Quest.EndDate < now)
                .ToListAsync();
        }
    }
}
