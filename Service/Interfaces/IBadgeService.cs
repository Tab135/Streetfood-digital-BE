using BO.DTO;
using BO.DTO.Badge;
using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IBadgeService
    {
        // Admin operations
        Task<BadgeDto> CreateBadge(CreateBadgeDto createBadgeDto);
        Task<BadgeDto> UpdateBadge(int badgeId, UpdateBadgeDto updateBadgeDto);
        Task<bool> DeleteBadge(int badgeId);
        
        // Badge queries
        Task<BadgeDto?> GetBadgeById(int badgeId);
        Task<List<BadgeDto>> GetAllBadges();
        
        // User badge operations
        Task<List<BadgeWithUserInfoDto>> GetUserBadgesWithInfo(int userId);
        Task CheckAndAwardBadges(int userId);
        Task<UserBadgeDto> AwardBadgeToUser(int userId, int badgeId);
        Task<int> GetUserBadgeCount(int userId);
    }
}
