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
                PointToGet = 100,
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
        public async Task CheckAndAwardBadges_ReturnsOk()
        {
            // Arrange
            _badgeServiceMock.Setup(s => s.CheckAndAwardBadges(1)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CheckAndAwardBadges();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            var msg = value?.GetType().GetProperty("message")?.GetValue(value) as string;
            Assert.Equal("Badges checked and awarded successfully", msg);
            _badgeServiceMock.Verify(s => s.CheckAndAwardBadges(1), Times.Once);
        }
    }
}
