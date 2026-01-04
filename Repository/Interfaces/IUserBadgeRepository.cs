using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IUserBadgeRepository
    {
        Task<UserBadge> Create(UserBadge userBadge);
        Task<UserBadge?> GetByUserAndBadge(int userId, int badgeId);
        Task<List<UserBadge>> GetByUserId(int userId);
        Task<List<int>> GetBadgeIdsByUserId(int userId);
        Task<bool> Exists(int userId, int badgeId);
        Task<int> GetUserBadgeCount(int userId);
        Task<bool> Delete(int userId, int badgeId);
    }
}
