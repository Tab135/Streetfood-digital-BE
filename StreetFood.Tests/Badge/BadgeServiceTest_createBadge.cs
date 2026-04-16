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
    public class BadgeServiceTest_createBadge
    {
        private Mock<IBadgeRepository> _badgeRepoMock;
        private Mock<IUserBadgeRepository> _userBadgeRepoMock;
        private Mock<IUserRepository> _userRepoMock;
        private BadgeService _badgeService;

        public BadgeServiceTest_createBadge()
        {
            _badgeRepoMock = new Mock<IBadgeRepository>();
            _userBadgeRepoMock = new Mock<IUserBadgeRepository>();
            _userRepoMock = new Mock<IUserRepository>();

            _badgeService = new BadgeService(
                _badgeRepoMock.Object,
                _userBadgeRepoMock.Object,
                _userRepoMock.Object
            );
        }

        [Fact]
        public async Task CreateBadge_UTCID01_Normal_ReturnsDto()
        {
            // Arrange
            var dto = new CreateBadgeDto { BadgeName = "New Badge", Description = "Standard Description" };
            var icon = "http://icon.com/img.png";
            _badgeRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<BO.Entities.Badge>()); // No duplicates
            _badgeRepoMock.Setup(r => r.Create(It.IsAny<BO.Entities.Badge>()))
                .ReturnsAsync((BO.Entities.Badge b) => { b.BadgeId = 1; return b; });

            // Act
            var result = await _badgeService.CreateBadge(dto, icon);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Badge", result.BadgeName);
            Assert.Equal(icon, result.IconUrl);
        }

        [Fact]
        public async Task CreateBadge_UTCID02_EmptyName_ThrowsException()
        {
            // Arrange
            var dto = new CreateBadgeDto { BadgeName = "" };
            
            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _badgeService.CreateBadge(dto, "icon"));
            Assert.Contains("Tên không để trống", ex.Message);
        }

        [Fact]
        public async Task CreateBadge_UTCID03_NameTooLong_ThrowsException()
        {
            // Arrange
            var dto = new CreateBadgeDto { BadgeName = new string('A', 300) };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _badgeService.CreateBadge(dto, "icon"));
            Assert.Contains("Tên quá dài", ex.Message);
        }

        [Fact]
        public async Task CreateBadge_UTCID04_IconMissing_ThrowsException()
        {
            // Arrange
            var dto = new CreateBadgeDto { BadgeName = "Valid Name" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _badgeService.CreateBadge(dto, " "));
            Assert.Contains("Icon không để trống", ex.Message);
        }

        [Fact]
        public async Task CreateBadge_UTCID05_DescriptionTooLong_ThrowsException()
        {
            // Arrange
            var dto = new CreateBadgeDto { BadgeName = "Valid Name", Description = new string('D', 1100) };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _badgeService.CreateBadge(dto, "icon"));
            Assert.Contains("Mô tả quá dài", ex.Message);
        }

        [Fact]
        public async Task CreateBadge_UTCID06_DuplicateName_ThrowsException()
        {
            // Arrange
            var dto = new CreateBadgeDto { BadgeName = "Existing Badge" };
            var existingBadges = new List<BO.Entities.Badge> { new BO.Entities.Badge { BadgeName = "Existing Badge" } };
            _badgeRepoMock.Setup(r => r.GetAll()).ReturnsAsync(existingBadges);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _badgeService.CreateBadge(dto, "icon"));
            Assert.Contains("Tên huy hiệu đã tồn tại", ex.Message);
        }

        [Fact]
        public async Task CreateBadge_UTCID07_MinimalDescription_ReturnsDto()
        {
            // Arrange
            var dto = new CreateBadgeDto { BadgeName = "Minimal", Description = null };
            _badgeRepoMock.Setup(r => r.GetAll()).ReturnsAsync(new List<BO.Entities.Badge>());
            _badgeRepoMock.Setup(r => r.Create(It.IsAny<BO.Entities.Badge>()))
                .ReturnsAsync((BO.Entities.Badge b) => { b.BadgeId = 7; return b; });

            // Act
            var result = await _badgeService.CreateBadge(dto, "icon");

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Description);
        }
    }
}
