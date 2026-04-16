using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BO.DTO;
using BO.DTO.Badge;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Service.Interfaces;
using StreetFood.Controllers;
using StreetFood.Services;
using Xunit;

namespace StreetFood.Tests.Badge
{
    public class BadgeControllerTests
    {
        private Mock<IBadgeService> _badgeServiceMock;
        private Mock<IS3Service> _s3ServiceMock;
        private BadgeController _controller;

        public BadgeControllerTests()
        {
            _badgeServiceMock = new Mock<IBadgeService>();
            _s3ServiceMock = new Mock<IS3Service>();

            _controller = new BadgeController(_badgeServiceMock.Object, _s3ServiceMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "User")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        private BadgeDto MakeSampleBadgeDto(int id = 1)
        {
            return new BadgeDto
            {
                BadgeId = id,
                BadgeName = "Test Badge",
                IconUrl = "http://example.com/icon.png",
                Description = "A test badge"
            };
        }

        [Fact]
        public async Task CreateBadge_ValidRequest_ReturnsCreated()
        {
            // Arrange
            var createDto = new CreateBadgeDto { BadgeName = "New Badge" };
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(100);
            
            _s3ServiceMock.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync("http://example.com/new.png");
            
            var badgeDto = MakeSampleBadgeDto(5);
            _badgeServiceMock.Setup(s => s.CreateBadge(createDto, "http://example.com/new.png"))
                .ReturnsAsync(badgeDto);

            // Act
            var result = await _controller.CreateBadge(createDto, fileMock.Object);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal("GetBadgeById", createdResult.ActionName);
            Assert.Equal(5, ((BadgeDto)createdResult.Value!).BadgeId);
        }

        [Fact]
        public async Task CreateBadge_NoImage_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new CreateBadgeDto { BadgeName = "New Badge" };

            // Act
            var result = await _controller.CreateBadge(createDto, null!);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Badge image is required", badRequest.Value!.ToString());
        }

        [Fact]
        public async Task UpdateBadge_ValidRequest_ReturnsOk()
        {
            // Arrange
            var updateDto = new UpdateBadgeDto { BadgeName = "Updated Badge" };
            var badgeDto = MakeSampleBadgeDto(1);
            badgeDto.BadgeName = "Updated Badge";

            _badgeServiceMock.Setup(s => s.UpdateBadge(1, updateDto, null)).ReturnsAsync(badgeDto);

            // Act
            var result = await _controller.UpdateBadge(1, updateDto, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            var msg = value?.GetType().GetProperty("message")?.GetValue(value) as string;
            Assert.Equal("Badge updated successfully", msg);
        }

        [Fact]
        public async Task DeleteBadge_Existing_ReturnsOk()
        {
            // Arrange
            _badgeServiceMock.Setup(s => s.DeleteBadge(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteBadge(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            var msg = value?.GetType().GetProperty("message")?.GetValue(value) as string;
            Assert.Equal("Badge deleted successfully", msg);
        }

        [Fact]
        public async Task DeleteBadge_NonExisting_ReturnsNotFound()
        {
            // Arrange
            _badgeServiceMock.Setup(s => s.DeleteBadge(99)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteBadge(99);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var value = notFound.Value;
            var msg = value?.GetType().GetProperty("message")?.GetValue(value) as string;
            Assert.Equal("Badge not found", msg);
        }

        [Fact]
        public async Task GetBadgeById_Existing_ReturnsOk()
        {
            // Arrange
            var badgeDto = MakeSampleBadgeDto(1);
            _badgeServiceMock.Setup(s => s.GetBadgeById(1)).ReturnsAsync(badgeDto);

            // Act
            var result = await _controller.GetBadgeById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, ((BadgeDto)okResult.Value!).BadgeId);
        }

        [Fact]
        public async Task GetBadgeById_NonExisting_ReturnsNotFound()
        {
            // Arrange
            _badgeServiceMock.Setup(s => s.GetBadgeById(99)).ReturnsAsync((BadgeDto?)null);

            // Act
            var result = await _controller.GetBadgeById(99);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var value = notFound.Value;
            var msg = value?.GetType().GetProperty("message")?.GetValue(value) as string;
            Assert.Equal("Badge not found", msg);
        }

        [Fact]
        public async Task GetAllBadges_ReturnsOk()
        {
            // Arrange
            var badges = new List<BadgeDto> { MakeSampleBadgeDto(1), MakeSampleBadgeDto(2) };
            _badgeServiceMock.Setup(s => s.GetAllBadges()).ReturnsAsync(badges);

            // Act
            var result = await _controller.GetAllBadges();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<List<BadgeDto>>(okResult.Value);
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public async Task GetAllUsersWithBadges_ReturnsOk()
        {
            // Arrange
            var response = new BO.Common.PaginatedResponse<UserWithBadgesDto>();
            _badgeServiceMock.Setup(s => s.GetAllUsersWithBadges(1, 10)).ReturnsAsync(response);

            // Act
            var result = await _controller.GetAllUsersWithBadges(1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, okResult.Value);
        }

        [Fact]
        public async Task GetUserBadges_ReturnsOk()
        {
            // Arrange - User claim is "1" from constructor
            var badges = new List<BadgeWithUserInfoDto>();
            _badgeServiceMock.Setup(s => s.GetUserBadgesWithInfo(1)).ReturnsAsync(badges);

            // Act
            var result = await _controller.GetUserBadges();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(badges, okResult.Value);
        }

        [Fact]
        public async Task AwardBadgeToUser_Valid_ReturnsOk()
        {
            // Arrange
            var userBadge = new UserBadgeDto { UserId = 1, BadgeId = 2 };
            _badgeServiceMock.Setup(s => s.AwardBadgeToUser(1, 2)).ReturnsAsync(userBadge);

            // Act
            var result = await _controller.AwardBadgeToUser(1, 2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            var msg = value?.GetType().GetProperty("message")?.GetValue(value) as string;
            Assert.Equal("Badge awarded successfully", msg);
        }

        [Fact]
        public async Task RemoveBadgeFromUser_Existing_ReturnsOk()
        {
            // Arrange
            _badgeServiceMock.Setup(s => s.RemoveBadgeFromUser(1, 2)).ReturnsAsync(true);

            // Act
            var result = await _controller.RemoveBadgeFromUser(1, 2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            var msg = value?.GetType().GetProperty("message")?.GetValue(value) as string;
            Assert.Equal("Badge removed from user successfully", msg);
        }

        [Fact]
        public async Task RemoveBadgeFromUser_NonExisting_ReturnsNotFound()
        {
            // Arrange
            _badgeServiceMock.Setup(s => s.RemoveBadgeFromUser(1, 99)).ReturnsAsync(false);

            // Act
            var result = await _controller.RemoveBadgeFromUser(1, 99);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var value = notFound.Value;
            var msg = value?.GetType().GetProperty("message")?.GetValue(value) as string;
            Assert.Equal("User badge not found", msg);
        }

        [Fact]
        public async Task GetBadgesByUserId_ReturnsOk()
        {
            // Arrange
            var badges = new List<BadgeWithUserInfoDto>();
            _badgeServiceMock.Setup(s => s.GetUserBadgesWithInfo(5)).ReturnsAsync(badges);

            // Act
            var result = await _controller.GetBadgesByUserId(5);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(badges, okResult.Value);
        }

        [Fact]
        public async Task GetUserBadgeCount_Authorized_ReturnsOk()
        {
            // Arrange
            _badgeServiceMock.Setup(s => s.GetUserBadgeCount(1)).ReturnsAsync(3);

            // Act
            var result = await _controller.GetUserBadgeCount(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            var count = value?.GetType().GetProperty("badgeCount")?.GetValue(value);
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task GetUserBadgeCount_Unauthorized_ReturnsForbid()
        {
            // Arrange
            // Target user is 5, but current logged in user is 1, and role is User (from constructor)
            // Wait, we need to ensure the claim matches what the controller expects. Controller checks currentUserId != userId && userRole != "Admin".
            // Since role is "User" in the mock, and current user is 1, requesting user 5 should Forbid.

            // Act
            var result = await _controller.GetUserBadgeCount(5);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

    }
}
