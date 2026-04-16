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

        private BO.Entities.Badge MakeSampleBadge(int id = 1, string name = "Beginner")
        {
            return new BO.Entities.Badge
            {
                BadgeId = id,
                BadgeName = name,
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
            var createDto = new CreateBadgeDto { BadgeName = "Pro", Description = "Pro badge" };
            var iconUrl = "http://example.com/pro.png";

            _badgeRepoMock.Setup(r => r.Create(It.IsAny<BO.Entities.Badge>()))
                .ReturnsAsync((BO.Entities.Badge b) => { b.BadgeId = 10; return b; });

            // Act
            var result = await _badgeService.CreateBadge(createDto, iconUrl);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.BadgeId);
            Assert.Equal("Pro", result.BadgeName);
            Assert.Equal(iconUrl, result.IconUrl);
        }

        [Fact]
        public async Task UpdateBadge_UTCID01_NormalUpdate_ReturnsUpdatedDto()
        {
            // Arrange
            var existingBadge = MakeSampleBadge(1, "Old Name");
            var updateDto = new UpdateBadgeDto { BadgeName = "Success Name", Description = "New Desc" };
            
            _badgeRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(existingBadge);
            _badgeRepoMock.Setup(r => r.Update(It.IsAny<BO.Entities.Badge>())).ReturnsAsync((BO.Entities.Badge b) => b);

            // Act
            var result = await _badgeService.UpdateBadge(1, updateDto, null);

            // Assert
            Assert.Equal("Success Name", result.BadgeName);
            Assert.Equal("New Desc", result.Description);
        }

        [Fact]
        public async Task UpdateBadge_UTCID02_UpdateIconOnly_ReturnsUpdatedDto()
        {
            // Arrange
            var existingBadge = MakeSampleBadge(1);
            var updateDto = new UpdateBadgeDto { BadgeName = existingBadge.BadgeName };
            var newIcon = "http://new-icon.com/image.png";

            _badgeRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(existingBadge);
            _badgeRepoMock.Setup(r => r.Update(It.IsAny<BO.Entities.Badge>())).ReturnsAsync((BO.Entities.Badge b) => b);

            // Act
            var result = await _badgeService.UpdateBadge(1, updateDto, newIcon);

            // Assert
            Assert.Equal(newIcon, result.IconUrl);
        }

        [Fact]
        public async Task UpdateBadge_UTCID03_BadgeNotFound_ThrowsDomainException()
        {
            // Arrange
            var updateDto = new UpdateBadgeDto { BadgeName = "Any" };
            _badgeRepoMock.Setup(r => r.GetById(999)).ReturnsAsync((BO.Entities.Badge?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _badgeService.UpdateBadge(999, updateDto, null));
            Assert.Contains("Không tìm thấy huy hiệu", ex.Message);
        }

        [Fact]
        public async Task UpdateBadge_UTCID04_EmptyName_ThrowsDomainException()
        {
            // Arrange
            var updateDto = new UpdateBadgeDto { BadgeName = " " };
            var existingBadge = MakeSampleBadge(1);
            _badgeRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(existingBadge);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _badgeService.UpdateBadge(1, updateDto, null));
            Assert.Contains("Tên không để trống", ex.Message);
        }

        [Fact]
        public async Task UpdateBadge_UTCID05_MaxLengthName_ReturnsUpdatedDto()
        {
            // Arrange
            string maxName = new string('A', 255);
            var updateDto = new UpdateBadgeDto { BadgeName = maxName };
            var existingBadge = MakeSampleBadge(1);
            
            _badgeRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(existingBadge);
            _badgeRepoMock.Setup(r => r.Update(It.IsAny<BO.Entities.Badge>())).ReturnsAsync((BO.Entities.Badge b) => b);

            // Act
            var result = await _badgeService.UpdateBadge(1, updateDto, null);

            // Assert
            Assert.Equal(maxName, result.BadgeName);
        }

        [Fact]
        public async Task UpdateBadge_UTCID06_NullDto_ThrowsDomainException()
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _badgeService.UpdateBadge(1, null!, null));
            Assert.Contains("Dữ liệu không hợp lệ", ex.Message);
        }

        [Fact]
        public async Task UpdateBadge_UTCID07_NoOpUpdate_ReturnsExistingDto()
        {
            // Arrange
            var existingBadge = MakeSampleBadge(1, "Original");
            var updateDto = new UpdateBadgeDto { BadgeName = "Original" };
            
            _badgeRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(existingBadge);
            _badgeRepoMock.Setup(r => r.Update(It.IsAny<BO.Entities.Badge>())).ReturnsAsync((BO.Entities.Badge b) => b);

            // Act
            var result = await _badgeService.UpdateBadge(1, updateDto, null);

            // Assert
            Assert.Equal("Original", result.BadgeName);
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

        [Fact]
        public async Task AwardBadgeToUser_UserNotFound_ThrowsDomainException()
        {
            // Arrange
            _userRepoMock.Setup(r => r.GetUserById(999)).ReturnsAsync((User?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _badgeService.AwardBadgeToUser(999, 1));
            Assert.Contains("Không tìm thấy người dùng", ex.Message);
        }

        [Fact]
        public async Task AwardBadgeToUser_BadgeNotFound_ThrowsDomainException()
        {
            // Arrange
            var user = MakeSampleUser(1);
            _userRepoMock.Setup(r => r.GetUserById(1)).ReturnsAsync(user);
            _badgeRepoMock.Setup(r => r.GetById(999)).ReturnsAsync((BO.Entities.Badge?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _badgeService.AwardBadgeToUser(1, 999));
            Assert.Contains("Không tìm thấy huy hiệu", ex.Message);
        }

        [Fact]
        public async Task RemoveBadgeFromUser_UserHasBadge_ReturnsTrue()
        {
            // Arrange
            _userBadgeRepoMock.Setup(r => r.Exists(1, 1)).ReturnsAsync(true);
            _userBadgeRepoMock.Setup(r => r.Delete(1, 1)).ReturnsAsync(true);

            // Act
            var result = await _badgeService.RemoveBadgeFromUser(1, 1);

            // Assert
            Assert.True(result);
            _userBadgeRepoMock.Verify(r => r.Delete(1, 1), Times.Once);
        }

        [Fact]
        public async Task RemoveBadgeFromUser_UserDoesNotHaveBadge_ThrowsDomainException()
        {
            // Arrange
            _userBadgeRepoMock.Setup(r => r.Exists(1, 99)).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _badgeService.RemoveBadgeFromUser(1, 99));
            Assert.Contains("Không tìm thấy huy hiệu của người dùng", ex.Message);
        }

        [Fact]
        public async Task GetUserBadgeCount_ReturnsCorrectCount()
        {
            // Arrange
            _userBadgeRepoMock.Setup(r => r.GetUserBadgeCount(1)).ReturnsAsync(5);

            // Act
            var result = await _badgeService.GetUserBadgeCount(1);

            // Assert
            Assert.Equal(5, result);
        }

        [Fact]
        public async Task GetUserBadgesWithInfo_UserNotFound_ThrowsDomainException()
        {
            // Arrange
            _userRepoMock.Setup(r => r.GetUserById(999)).ReturnsAsync((User?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _badgeService.GetUserBadgesWithInfo(999));
            Assert.Contains("Không tìm thấy người dùng", ex.Message);
        }

        [Fact]
        public async Task GetAllBadges_ReturnsAllBadges()
        {
            // Arrange
            var badges = new List<BO.Entities.Badge> { MakeSampleBadge(1), MakeSampleBadge(2) };
            _badgeRepoMock.Setup(r => r.GetAll()).ReturnsAsync(badges);

            // Act
            var result = await _badgeService.GetAllBadges();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetBadgeById_ExistingBadge_ReturnsDto()
        {
            // Arrange
            var badge = MakeSampleBadge(1);
            _badgeRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(badge);

            // Act
            var result = await _badgeService.GetBadgeById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result!.BadgeId);
        }
    }
}
