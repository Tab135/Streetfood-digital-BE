using BO.Entities;
using DAL;
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

        public async Task<UserBadge?> GetByUserAndBadge(int userId, int badgeId)
        {
            return await _userBadgeDAO.GetByUserAndBadge(userId, badgeId);
        }

        public async Task<List<UserBadge>> GetByUserId(int userId)
        {
            return await _userBadgeDAO.GetByUserId(userId);
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
    }
}
