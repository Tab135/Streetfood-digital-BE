using BO.Common;
using BO.DTO.Vendor;
using BO.DTO.Dietary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Service.Interfaces;
using StreetFood.Controllers;
using StreetFood.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using BO.Entities;

namespace StreetFood.Tests.Vendor
{
    public class VendorControllerTests
    {
        private readonly Mock<IVendorService> _vendorServiceMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly Mock<IVendorDietaryPreferenceService> _dietaryPrefMock;
        private readonly Mock<IS3Service> _s3ServiceMock;
        private readonly VendorController _controller;

        public VendorControllerTests()
        {
            _vendorServiceMock = new Mock<IVendorService>();
            _envMock = new Mock<IWebHostEnvironment>();
            _dietaryPrefMock = new Mock<IVendorDietaryPreferenceService>();
            _s3ServiceMock = new Mock<IS3Service>();

            _controller = new VendorController(
                _vendorServiceMock.Object,
                _envMock.Object,
                _dietaryPrefMock.Object,
                _s3ServiceMock.Object
            );
            
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim(ClaimTypes.NameIdentifier, "1")
            }, "mock"));
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [Fact]
        public async Task GetVendorById_ReturnsOk_WhenVendorExists()
        {
            // Arrange
            var vendorDto = new VendorResponseDto { VendorId = 1, Name = "Vendor1" };
            _vendorServiceMock.Setup(s => s.GetVendorByIdAsync(1)).ReturnsAsync(vendorDto);

            // Act
            var result = await _controller.GetVendorById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedVendor = Assert.IsType<VendorResponseDto>(okResult.Value);
            Assert.Equal(1, returnedVendor.VendorId);
        }

        [Fact]
        public async Task GetVendorById_ReturnsNotFound_WhenVendorDoesNotExist()
        {
            // Arrange
            _vendorServiceMock.Setup(s => s.GetVendorByIdAsync(99)).ThrowsAsync(new Exception("Not found"));

            // Act
            var result = await _controller.GetVendorById(99);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Not found", notFoundResult.Value);
        }

        [Fact]
        public async Task CreateVendor_ReturnsCreated_WhenValid()
        {
            // Arrange
            var createDto = new CreateVendorDto { Name = "New Vendor", DietaryPreferenceIds = new List<int> { 1, 2 } };
            var vendorEnt = new BO.Entities.Vendor { VendorId = 10, Name = "New Vendor" };
            var vendorRes = new VendorResponseDto { VendorId = 10, Name = "New Vendor" };
            
            _vendorServiceMock.Setup(s => s.CreateVendorAsync(createDto, 1)).ReturnsAsync(vendorEnt);
            _dietaryPrefMock.Setup(s => s.AssignPreferencesToVendor(10, createDto.DietaryPreferenceIds)).ReturnsAsync(new List<DietaryPreferenceDto>());
            _vendorServiceMock.Setup(s => s.GetVendorByIdAsync(10)).ReturnsAsync(vendorRes);

            // Act
            var result = await _controller.CreateVendor(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var returnedVendor = Assert.IsType<VendorResponseDto>(createdResult.Value);
            Assert.Equal(10, returnedVendor.VendorId);
        }
        
        [Fact]
        public async Task DeleteVendor_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            _vendorServiceMock.Setup(s => s.DeleteVendorAsync(1)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteVendor(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
        }
        
        [Fact]
        public async Task GetMyVendor_ReturnsOk_WhenVendorExists()
        {
            // Arrange
            var vendorDto = new VendorResponseDto { VendorId = 1, Name = "My Vendor" };
            _vendorServiceMock.Setup(s => s.GetVendorByUserIdAsync(1)).ReturnsAsync(vendorDto);

            // Act
            var result = await _controller.GetMyVendor();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedVendor = Assert.IsType<VendorResponseDto>(okResult.Value);
            Assert.Equal(1, returnedVendor.VendorId);
        }
    }
}
