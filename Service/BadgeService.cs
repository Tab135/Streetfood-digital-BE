using BO.Common;
using BO.DTO;
using BO.DTO.Badge;
using BO.Entities;
using BO.Exceptions;
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
        public async Task<BadgeDto> CreateBadge(CreateBadgeDto createBadgeDto, string iconUrl)
        {
            var badge = new Badge
            {
                BadgeName = createBadgeDto.BadgeName,
                IconUrl = iconUrl,
                Description = createBadgeDto.Description
            };

            var createdBadge = await _badgeRepository.Create(badge);

            return MapToDto(createdBadge);
        }

        public async Task<BadgeDto> UpdateBadge(int badgeId, UpdateBadgeDto updateBadgeDto, string? iconUrl)
        {
            var badge = await _badgeRepository.GetById(badgeId);
            if (badge == null)
                throw new DomainExceptions($"Không tìm thấy huy hiệu với ID {badgeId}");

            if (!string.IsNullOrEmpty(updateBadgeDto.BadgeName))
                badge.BadgeName = updateBadgeDto.BadgeName;

            if (!string.IsNullOrWhiteSpace(iconUrl))
                badge.IconUrl = iconUrl;

            if (updateBadgeDto.Description != null)
                badge.Description = updateBadgeDto.Description;

            var updatedBadge = await _badgeRepository.Update(badge);

            return MapToDto(updatedBadge);
        }

        public async Task<bool> DeleteBadge(int badgeId)
        {
            var entity = await _badgeRepository.GetById(badgeId);
            if (entity == null)
                throw new DomainExceptions($"Không tìm thấy huy hiệu với ID {badgeId}");

            if (entity.IsActive)
            {
                var isInUse = await _badgeRepository.IsInUseAsync(badgeId);
                if (isInUse)
                    throw new DomainExceptions($"Không thể vô hiệu hóa huy hiệu này vì đang được sử dụng bởi người dùng hoặc nhiệm vụ");
            }

            var newStatus = !entity.IsActive;
            await _badgeRepository.UpdateIsActiveAsync(badgeId, newStatus);
            return true;
        }


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
        public async Task<List<BadgeWithUserInfoDto>> GetUserBadgesWithInfo(int userId)
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
                throw new DomainExceptions($"Không tìm thấy người dùng với ID {userId}");
            return await _userBadgeRepository.GetUserBadgesWithInfo(userId);
        }

        public async Task<PaginatedResponse<UserWithBadgesDto>> GetAllUsersWithBadges(int pageNumber, int pageSize)
        {
            var (items, totalCount) = await _userBadgeRepository.GetAllUsersWithBadges(pageNumber, pageSize);
            return new PaginatedResponse<UserWithBadgesDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<UserBadgeDto> AwardBadgeToUser(int userId, int badgeId)
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
                throw new DomainExceptions($"Không tìm thấy người dùng với ID {userId}");

            var badge = await _badgeRepository.GetById(badgeId);
            if (badge == null)
                throw new DomainExceptions($"Không tìm thấy huy hiệu với ID {badgeId}");

            var exists = await _userBadgeRepository.Exists(userId, badgeId);
            if (exists)
                throw new DomainExceptions($"Người dùng đã có huy hiệu này rồi");

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

        public async Task<bool> RemoveBadgeFromUser(int userId, int badgeId)
        {
            var exists = await _userBadgeRepository.Exists(userId, badgeId);
            if (!exists)
                throw new DomainExceptions($"Không tìm thấy huy hiệu của người dùng");

            return await _userBadgeRepository.Delete(userId, badgeId);
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
                IconUrl = badge.IconUrl,
                IsActive = badge.IsActive,
                Description = badge.Description
            };
        }
    }
}
