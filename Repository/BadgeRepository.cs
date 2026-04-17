using BO.Entities;
using DAL;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class BadgeRepository : IBadgeRepository
    {
        private readonly BadgeDAO _badgeDAO;

        public BadgeRepository(BadgeDAO badgeDAO)
        {
            _badgeDAO = badgeDAO ?? throw new ArgumentNullException(nameof(badgeDAO));
        }

        public async Task<Badge> Create(Badge badge)
        {
            return await _badgeDAO.Create(badge);
        }

        public async Task<Badge?> GetById(int badgeId)
        {
            return await _badgeDAO.GetById(badgeId);
        }

        public async Task<List<Badge>> GetAll()
        {
            return await _badgeDAO.GetAll();
        }

        public async Task<Badge> Update(Badge badge)
        {
            return await _badgeDAO.Update(badge);
        }

        public async Task<bool> IsInUseAsync(int badgeId)
        {
            return await _badgeDAO.IsInUseAsync(badgeId);
        }

        public async Task<Badge> UpdateIsActiveAsync(int badgeId, bool isActive)
        {
            return await _badgeDAO.UpdateIsActiveAsync(badgeId, isActive);
        }
    }
}
