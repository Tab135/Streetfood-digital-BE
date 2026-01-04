using BO.DTO;
using BO.DTO.Badge;
using BO.Entities;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service
{
    public class BadgeService : IBadgeService
    {
        private readonly IBadgeRepository _badgeRepository;
        private readonly IUserBadgeRepository _userBadgeRepository;
        private readonly IUserRepository _userRepository;

        public BadgeService(IBadgeRepository badgeRepository,IUserBadgeRepository userBadgeRepository,IUserRepository userRepository)
        {
            _badgeRepository = badgeRepository;
            _userBadgeRepository = userBadgeRepository;
            _userRepository = userRepository;
        }

        // Admin operations
        public async Task<BadgeDto> CreateBadge(CreateBadgeDto createBadgeDto)
        {
            var badge = new Badge
            {
                BadgeName = createBadgeDto.BadgeName,
                PointToGet = createBadgeDto.PointToGet,
                IconUrl = createBadgeDto.IconUrl,
                Description = createBadgeDto.Description
            };

            var createdBadge = await _badgeRepository.Create(badge);

            return MapToDto(createdBadge);
        }

        public async Task<BadgeDto> UpdateBadge(int badgeId, UpdateBadgeDto updateBadgeDto)
        {
            var badge = await _badgeRepository.GetById(badgeId);
            if (badge == null)
                throw new Exception($"Badge with ID {badgeId} not found");

            if (!string.IsNullOrEmpty(updateBadgeDto.BadgeName))
                badge.BadgeName = updateBadgeDto.BadgeName;

            if (updateBadgeDto.PointToGet.HasValue)
                badge.PointToGet = updateBadgeDto.PointToGet.Value;

            if (!string.IsNullOrEmpty(updateBadgeDto.IconUrl))
                badge.IconUrl = updateBadgeDto.IconUrl;

            if (updateBadgeDto.Description != null)
                badge.Description = updateBadgeDto.Description;

            var updatedBadge = await _badgeRepository.Update(badge);

            return MapToDto(updatedBadge);
        }

        public async Task<bool> DeleteBadge(int badgeId)
        {
            var exists = await _badgeRepository.Exists(badgeId);
            if (!exists)
                throw new Exception($"Badge with ID {badgeId} not found");

            return await _badgeRepository.Delete(badgeId);
        }

        // Badge queries
        public async Task<BadgeDto?> GetBadgeById(int badgeId)
        {
            var badge = await _badgeRepository.GetById(badgeId);
            return badge == null ? null : MapToDto(badge);
        }

        public async Task<List<BadgeDto>> GetAllBadges()
        {
            var badges = await _badgeRepository.GetAll();
            return badges.Select(MapToDto).ToList();
        }

        // User badge operations
        public async Task<List<BadgeWithUserInfoDto>> GetUserBadgesWithInfo(int userId)
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
                throw new Exception($"User with ID {userId} not found");

            var allBadges = await _badgeRepository.GetAll();
            var earnedBadgeIds = await _userBadgeRepository.GetBadgeIdsByUserId(userId);
            var userBadges = await _userBadgeRepository.GetByUserId(userId);

            var badgeWithInfo = allBadges.Select(badge =>
            {
                var userBadge = userBadges.FirstOrDefault(ub => ub.BadgeId == badge.BadgeId);
                return new BadgeWithUserInfoDto
                {
                    BadgeId = badge.BadgeId,
                    BadgeName = badge.BadgeName,
                    PointToGet = badge.PointToGet,
                    IconUrl = badge.IconUrl,
                    Description = badge.Description,
                    IsEarned = earnedBadgeIds.Contains(badge.BadgeId),
                    EarnedAt = userBadge?.CreatedAt
                };
            }).ToList();

            return badgeWithInfo;
        }

        public async Task CheckAndAwardBadges(int userId)
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
                throw new Exception($"User with ID {userId} not found");

            // Get all badges that the user qualifies for based on points
            var eligibleBadges = await _badgeRepository.GetBadgesByPointThreshold(user.Point);
            
            // Get badges the user already has
            var earnedBadgeIds = await _userBadgeRepository.GetBadgeIdsByUserId(userId);

            // Award new badges
            foreach (var badge in eligibleBadges)
            {
                if (!earnedBadgeIds.Contains(badge.BadgeId))
                {
                    var userBadge = new UserBadge
                    {
                        UserId = userId,
                        BadgeId = badge.BadgeId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _userBadgeRepository.Create(userBadge);
                }
            }
        }

        public async Task<UserBadgeDto> AwardBadgeToUser(int userId, int badgeId)
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
                throw new Exception($"User with ID {userId} not found");

            var badge = await _badgeRepository.GetById(badgeId);
            if (badge == null)
                throw new Exception($"Badge with ID {badgeId} not found");

            var exists = await _userBadgeRepository.Exists(userId, badgeId);
            if (exists)
                throw new Exception($"User already has this badge");

            var userBadge = new UserBadge
            {
                UserId = userId,
                BadgeId = badgeId,
                CreatedAt = DateTime.UtcNow
            };

            var createdUserBadge = await _userBadgeRepository.Create(userBadge);

            return new UserBadgeDto
            {
                UserId = createdUserBadge.UserId,
                BadgeId = createdUserBadge.BadgeId,
                CreatedAt = createdUserBadge.CreatedAt
            };
        }

        public async Task<int> GetUserBadgeCount(int userId)
        {
            return await _userBadgeRepository.GetUserBadgeCount(userId);
        }

        // Helper methods
        private BadgeDto MapToDto(Badge badge)
        {
            return new BadgeDto
            {
                BadgeId = badge.BadgeId,
                BadgeName = badge.BadgeName,
                PointToGet = badge.PointToGet,
                IconUrl = badge.IconUrl,
                Description = badge.Description
            };
        }
    }
}
