using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class UserBadgeDAO
    {
        private readonly StreetFoodDbContext _context;

        public UserBadgeDAO(StreetFoodDbContext context)
        {
            _context = context;
        }

        public async Task<UserBadge> Create(UserBadge userBadge)
        {
            _context.UserBadges.Add(userBadge);
            await _context.SaveChangesAsync();
            return userBadge;
        }

        public async Task<UserBadge?> GetByUserAndBadge(int userId, int badgeId)
        {
            return await _context.UserBadges
                .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId);
        }

        public async Task<List<UserBadge>> GetByUserId(int userId)
        {
            return await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .OrderByDescending(ub => ub.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<int>> GetBadgeIdsByUserId(int userId)
        {
            return await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.BadgeId)
                .ToListAsync();
        }

        public async Task<bool> Exists(int userId, int badgeId)
        {
            return await _context.UserBadges
                .AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId);
        }

        public async Task<int> GetUserBadgeCount(int userId)
        {
            return await _context.UserBadges
                .CountAsync(ub => ub.UserId == userId);
        }

        public async Task<bool> Delete(int userId, int badgeId)
        {
            var userBadge = await GetByUserAndBadge(userId, badgeId);
            if (userBadge == null)
                return false;

            _context.UserBadges.Remove(userBadge);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
