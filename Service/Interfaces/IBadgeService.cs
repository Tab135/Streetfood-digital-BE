using BO.Common;
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
        Task<BadgeDto> CreateBadge(CreateBadgeDto createBadgeDto, string iconUrl);
        Task<BadgeDto> UpdateBadge(int badgeId, UpdateBadgeDto updateBadgeDto, string? iconUrl);
        Task<bool> DeleteBadge(int badgeId);
        
        // Badge queries
        Task<BadgeDto?> GetBadgeById(int badgeId);
        Task<List<BadgeDto>> GetAllBadges();
        
        // User badge operations
        Task<List<BadgeWithUserInfoDto>> GetUserBadgesWithInfo(int userId);
        Task<PaginatedResponse<UserWithBadgesDto>> GetAllUsersWithBadges(int pageNumber, int pageSize);
        Task<UserBadgeDto> AwardBadgeToUser(int userId, int badgeId);
        Task<bool> RemoveBadgeFromUser(int userId, int badgeId);
        Task<int> GetUserBadgeCount(int userId);
        Task<bool> SelectDisplayBadge(int userId, int badgeId);
        Task<bool> ClearDisplayBadge(int userId);
    }
}
