using BO.Entities;
using BO.DTO.Badge;
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
            var userBadge = await _context.UserBadges
                .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId);
            
            if (userBadge == null)
                return false;

            _context.UserBadges.Remove(userBadge);
            await _context.SaveChangesAsync();
            return true;
        }

        // Query method for getting all users with their badges using database join
        public async Task<(List<UserWithBadgesDto> items, int totalCount)> GetAllUsersWithBadges(int pageNumber, int pageSize)
        {
            var query = _context.Users;

            var totalCount = await query.CountAsync();

            var result = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(user => new UserWithBadgesDto
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Point = user.Point,
                    Badges = _context.UserBadges
                        .Where(ub => ub.UserId == user.Id)
                        .Join(
                            _context.Badges,
                            ub => ub.BadgeId,
                            badge => badge.BadgeId,
                            (ub, badge) => new BadgeWithUserInfoDto
                            {
                                BadgeId = badge.BadgeId,
                                BadgeName = badge.BadgeName,
                                IconUrl = badge.IconUrl,
                                Description = badge.Description,
                                IsEarned = true,
                                EarnedAt = ub.CreatedAt
                            })
                        .ToList()
                })
                .ToListAsync();

            return (result, totalCount);
        }

        // Query method for getting a specific user's badges with info using database join
        public async Task<List<BadgeWithUserInfoDto>> GetUserBadgesWithInfo(int userId)
        {
            var result = await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .Join(
                    _context.Badges,
                    ub => ub.BadgeId,
                    badge => badge.BadgeId,
                    (ub, badge) => new BadgeWithUserInfoDto
                    {
                        BadgeId = badge.BadgeId,
                        BadgeName = badge.BadgeName,
                        IconUrl = badge.IconUrl,
                        Description = badge.Description,
                        IsEarned = true,
                        EarnedAt = ub.CreatedAt
                    })
                .ToListAsync();

            return result;
        }
    }
}
