using BO.Entities;
using BO.DTO.Badge;
using DAL;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class UserBadgeRepository : IUserBadgeRepository
    {
        private readonly UserBadgeDAO _userBadgeDAO;

        public UserBadgeRepository(UserBadgeDAO userBadgeDAO)
        {
            _userBadgeDAO = userBadgeDAO ?? throw new ArgumentNullException(nameof(userBadgeDAO));
        }

        public async Task<UserBadge> Create(UserBadge userBadge)
        {
            return await _userBadgeDAO.Create(userBadge);
        }

        public async Task<List<int>> GetBadgeIdsByUserId(int userId)
        {
            return await _userBadgeDAO.GetBadgeIdsByUserId(userId);
        }

        public async Task<bool> Exists(int userId, int badgeId)
        {
            return await _userBadgeDAO.Exists(userId, badgeId);
        }

        public async Task<int> GetUserBadgeCount(int userId)
        {
            return await _userBadgeDAO.GetUserBadgeCount(userId);
        }

        public async Task<bool> Delete(int userId, int badgeId)
        {
            return await _userBadgeDAO.Delete(userId, badgeId);
        }

        public async Task<(List<UserWithBadgesDto> items, int totalCount)> GetAllUsersWithBadges(int pageNumber, int pageSize)
        {
            return await _userBadgeDAO.GetAllUsersWithBadges(pageNumber, pageSize);
        }

        public async Task<List<BadgeWithUserInfoDto>> GetUserBadgesWithInfo(int userId)
        {
            return await _userBadgeDAO.GetUserBadgesWithInfo(userId);
        }
    }
}
