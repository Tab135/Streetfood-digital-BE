using BO.Entities;
using BO.DTO.Badge;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IUserBadgeRepository
    {
        Task<UserBadge> Create(UserBadge userBadge);
        Task<List<int>> GetBadgeIdsByUserId(int userId);
        Task<bool> Exists(int userId, int badgeId);
        Task<int> GetUserBadgeCount(int userId);
        Task<bool> Delete(int userId, int badgeId);
        Task<List<UserWithBadgesDto>> GetAllUsersWithBadges();
        Task<List<BadgeWithUserInfoDto>> GetUserBadgesWithInfo(int userId);
    }
}
