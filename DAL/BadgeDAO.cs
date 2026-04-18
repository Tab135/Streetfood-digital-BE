using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class BadgeDAO
    {
        private readonly StreetFoodDbContext _context;

        public BadgeDAO(StreetFoodDbContext context)
        {
            _context = context;
        }

        public async Task<Badge> Create(Badge badge)
        {
            _context.Badges.Add(badge);
            await _context.SaveChangesAsync();
            return badge;
        }

        public async Task<Badge?> GetById(int badgeId)
        {
            return await _context.Badges.FirstOrDefaultAsync(b => b.BadgeId == badgeId);
        }

        public async Task<List<Badge>> GetAll()
        {
            return await _context.Badges.OrderBy(b => b.BadgeId).ToListAsync();
        }

        public async Task<Badge> Update(Badge badge)
        {
            _context.Badges.Update(badge);
            await _context.SaveChangesAsync();
            return badge;
        }


        public async Task<bool> IsInUseAsync(int badgeId)
        {
            var usedByUser = await _context.UserBadges.AnyAsync(ub => ub.BadgeId == badgeId);
            if (usedByUser) return true;

            var usedInQuest = await _context.QuestTaskRewards.AnyAsync(qr => qr.RewardType == BO.Enums.QuestRewardType.BADGE && qr.RewardValue == badgeId);
            return usedInQuest;
        }

        public async Task<bool> UpdateIsActiveAsync(int badgeId, bool isActive)
        {
           var rowsAffected = await _context.Badges
                .Where(b => b.BadgeId == badgeId)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsActive, isActive));
            
            return rowsAffected > 0;
        }
    }
}