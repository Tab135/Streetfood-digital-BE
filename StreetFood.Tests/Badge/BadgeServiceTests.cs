using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BO.DTO.Badge;
using BO.Entities;
using BO.Exceptions;
using Moq;
using Repository.Interfaces;
using Service;
using Xunit;

namespace StreetFood.Tests.Badge
{
    public class BadgeServiceTests
    {
        private Mock<IBadgeRepository> _badgeRepoMock;
        private Mock<IUserBadgeRepository> _userBadgeRepoMock;
        private Mock<IUserRepository> _userRepoMock;
        private BadgeService _badgeService;

        public BadgeServiceTests()
        {
            _badgeRepoMock = new Mock<IBadgeRepository>();
            _userBadgeRepoMock = new Mock<IUserBadgeRepository>();
            _userRepoMock = new Mock<IUserRepository>();

            _badgeService = new Service.BadgeService(
                _badgeRepoMock.Object,
                _userBadgeRepoMock.Object,
                _userRepoMock.Object
            );
        }

        private BO.Entities.Badge MakeSampleBadge(int id = 1, string name = "Beginner", int points = 100)
        {
            return new BO.Entities.Badge
            {
                BadgeId = id,
                BadgeName = name,
                PointToGet = points,
                IconUrl = "http://example.com/icon.png",
                Description = "A badge"
            };
        }

        private User MakeSampleUser(int id = 1, int points = 150)
        {
            return new User
            {
                Id = id,
                UserName = "testuser",
                Point = points
            };
        }

        [Fact]
        public async Task CreateBadge_ReturnsMappedDto()
        {
            // Arrange
            var createDto = new CreateBadgeDto { BadgeName = "Pro", PointToGet = 500, Description = "Pro badge" };
            var iconUrl = "http://example.com/pro.png";

            _badgeRepoMock.Setup(r => r.Create(It.IsAny<BO.Entities.Badge>()))
                .ReturnsAsync((BO.Entities.Badge b) => { b.BadgeId = 10; return b; });

            // Act
            var result = await _badgeService.CreateBadge(createDto, iconUrl);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.BadgeId);
            Assert.Equal("Pro", result.BadgeName);
            Assert.Equal(500, result.PointToGet);
            Assert.Equal(iconUrl, result.IconUrl);
        }

        [Fact]
        public async Task UpdateBadge_ExistingBadge_UpdatesAndReturnsDto()
        {
            // Arrange
            var existingBadge = MakeSampleBadge(1);
            var updateDto = new UpdateBadgeDto { BadgeName = "New Name", PointToGet = 200, Description = "New Desc" };
            
            _badgeRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(existingBadge);
            _badgeRepoMock.Setup(r => r.Update(It.IsAny<BO.Entities.Badge>()))
                .ReturnsAsync((BO.Entities.Badge b) => b);

            // Act
            var result = await _badgeService.UpdateBadge(1, updateDto, "http://newicon.png");

            // Assert
            Assert.Equal("New Name", result.BadgeName);
            Assert.Equal(200, result.PointToGet);
            Assert.Equal("http://newicon.png", result.IconUrl);
            Assert.Equal("New Desc", result.Description);
        }

        [Fact]
        public async Task UpdateBadge_NonExistingBadge_ThrowsDomainException()
        {
            // Arrange
            var updateDto = new UpdateBadgeDto { BadgeName = "New Name" };
            _badgeRepoMock.Setup(r => r.GetById(99)).ReturnsAsync((BO.Entities.Badge?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _badgeService.UpdateBadge(99, updateDto, null));
            Assert.Contains("Không tìm thấy huy hiệu", ex.Message);
        }

        [Fact]
        public async Task DeleteBadge_ExistingBadge_ReturnsTrue()
        {
            // Arrange
            _badgeRepoMock.Setup(r => r.Exists(1)).ReturnsAsync(true);
            _badgeRepoMock.Setup(r => r.Delete(1)).ReturnsAsync(true);

            // Act
            var result = await _badgeService.DeleteBadge(1);

            // Assert
            Assert.True(result);
            _badgeRepoMock.Verify(r => r.Delete(1), Times.Once);
        }

        [Fact]
        public async Task DeleteBadge_NonExistingBadge_ThrowsDomainException()
        {
            // Arrange
            _badgeRepoMock.Setup(r => r.Exists(99)).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _badgeService.DeleteBadge(99));
            Assert.Contains("Không tìm thấy huy hiệu", ex.Message);
        }

        [Fact]
        public async Task CheckAndAwardBadges_UserEligibleForNewBadge_AwardsBadge()
        {
            // Arrange
            var user = MakeSampleUser(1, points: 200);
            _userRepoMock.Setup(r => r.GetUserById(1)).ReturnsAsync(user);

            var eligibleBadges = new List<BO.Entities.Badge> { MakeSampleBadge(10, points: 100), MakeSampleBadge(11, points: 200) };
            _badgeRepoMock.Setup(r => r.GetBadgesByPointThreshold(200)).ReturnsAsync(eligibleBadges);

            // User already has badge 10
            _userBadgeRepoMock.Setup(r => r.GetBadgeIdsByUserId(1)).ReturnsAsync(new List<int> { 10 });

            // Act
            await _badgeService.CheckAndAwardBadges(1);

            // Assert
            // It should only award badge 11 because badge 10 is already earned
            _userBadgeRepoMock.Verify(r => r.Create(It.Is<UserBadge>(ub => ub.UserId == 1 && ub.BadgeId == 11)), Times.Once);
            _userBadgeRepoMock.Verify(r => r.Create(It.Is<UserBadge>(ub => ub.BadgeId == 10)), Times.Never);
        }

        [Fact]
        public async Task AwardBadgeToUser_UserDoesNotHaveBadge_AwardsBadge()
        {
            // Arrange
            var user = MakeSampleUser(1);
            var badge = MakeSampleBadge(1);
            
            _userRepoMock.Setup(r => r.GetUserById(1)).ReturnsAsync(user);
            _badgeRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(badge);
            _userBadgeRepoMock.Setup(r => r.Exists(1, 1)).ReturnsAsync(false);
            
            _userBadgeRepoMock.Setup(r => r.Create(It.IsAny<UserBadge>()))
                .ReturnsAsync((UserBadge ub) => { ub.CreatedAt = DateTime.UtcNow; return ub; });

            // Act
            var result = await _badgeService.AwardBadgeToUser(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
            Assert.Equal(1, result.BadgeId);
            _userBadgeRepoMock.Verify(r => r.Create(It.IsAny<UserBadge>()), Times.Once);
        }

        [Fact]
        public async Task AwardBadgeToUser_UserAlreadyHasBadge_ThrowsDomainException()
        {
            // Arrange
            var user = MakeSampleUser(1);
            var badge = MakeSampleBadge(1);
            
            _userRepoMock.Setup(r => r.GetUserById(1)).ReturnsAsync(user);
            _badgeRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(badge);
            _userBadgeRepoMock.Setup(r => r.Exists(1, 1)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _badgeService.AwardBadgeToUser(1, 1));
            Assert.Contains("Người dùng đã có huy hiệu này", ex.Message);
        }
    }
}
