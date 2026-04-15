using BO.DTO.Dietary;
using BO.DTO.Vendor;
using BO.Entities;
using BO.Exceptions;
using Moq;
using Repository.Interfaces;
using Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace StreetFood.Tests.Vendor
{
    public class VendorServiceTests
    {
        private readonly Mock<IVendorRepository> _vendorRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IBranchRepository> _branchRepoMock;
        private readonly VendorService _vendorService;

        public VendorServiceTests()
        {
            _vendorRepoMock = new Mock<IVendorRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _branchRepoMock = new Mock<IBranchRepository>();

            _vendorService = new VendorService(
                _vendorRepoMock.Object,
                _userRepoMock.Object,
                _branchRepoMock.Object
            );
        }

        [Fact]
        public async Task CreateVendorAsync_UserNotFound_ThrowsDomainException()
        {
            // Arrange
            _userRepoMock.Setup(r => r.GetUserById(It.IsAny<int>())).ReturnsAsync((User?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _vendorService.CreateVendorAsync(new CreateVendorDto { DietaryPreferenceIds = new List<int>() }, 1));
            Assert.Contains("Không tìm thấy người dùng", ex.Message);
        }

        [Fact]
        public async Task CreateVendorAsync_UserAlreadyHasVendor_ThrowsDomainException()
        {
            // Arrange
            var user = new User { Id = 1 };
            _userRepoMock.Setup(r => r.GetUserById(1)).ReturnsAsync(user);
            _vendorRepoMock.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(new BO.Entities.Vendor());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _vendorService.CreateVendorAsync(new CreateVendorDto { DietaryPreferenceIds = new List<int>() }, 1));
            Assert.Contains("Đã có tài khoản cửa hàng", ex.Message);
        }

        [Fact]
        public async Task CreateVendorAsync_ValidData_CreatesVendorAndBranch()
        {
            // Arrange
            var user = new User { Id = 1 };
            var createDto = new CreateVendorDto { Name = "Test Vendor", BranchName = "Test Branch", DietaryPreferenceIds = new List<int>() };
            var createdVendor = new BO.Entities.Vendor { VendorId = 10, UserId = 1, Name = "Test Vendor" };
            var createdBranch = new Branch { BranchId = 20, VendorId = 10 };

            _userRepoMock.Setup(r => r.GetUserById(1)).ReturnsAsync(user);
            _vendorRepoMock.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync((BO.Entities.Vendor?)null);
            _vendorRepoMock.Setup(r => r.CreateAsync(It.IsAny<BO.Entities.Vendor>())).ReturnsAsync(createdVendor);
            _branchRepoMock.Setup(r => r.CreateAsync(It.IsAny<Branch>())).ReturnsAsync(createdBranch);
            _branchRepoMock.Setup(r => r.AddBranchRequestAsync(It.IsAny<BranchRequest>())).Returns(Task.CompletedTask);

            // Act
            var result = await _vendorService.CreateVendorAsync(createDto, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.VendorId);
            _vendorRepoMock.Verify(r => r.CreateAsync(It.IsAny<BO.Entities.Vendor>()), Times.Once);
            _branchRepoMock.Verify(r => r.CreateAsync(It.IsAny<Branch>()), Times.Once);
            _branchRepoMock.Verify(r => r.AddBranchRequestAsync(It.IsAny<BranchRequest>()), Times.Once);
        }

        [Fact]
        public async Task GetVendorByIdAsync_VendorNotFound_ThrowsDomainException()
        {
            // Arrange
            _vendorRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((BO.Entities.Vendor?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _vendorService.GetVendorByIdAsync(99));
            Assert.Contains("Không tìm thấy cửa hàng", ex.Message);
        }

        [Fact]
        public async Task GetVendorByIdAsync_VendorExists_ReturnsMappedDto()
        {
            // Arrange
            var vendor = new BO.Entities.Vendor { VendorId = 1, UserId = 1, Name = "My Vendor", VendorOwner = new User { FirstName="A", LastName="B" } };
            _vendorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vendor);
            _branchRepoMock.Setup(r => r.GetAllByVendorIdAsync(1)).ReturnsAsync(new List<Branch>());

            // Act
            var result = await _vendorService.GetVendorByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.VendorId);
            Assert.Equal("A B", result.VendorOwnerName);
        }
        
        [Fact]
        public async Task SuspendVendorAsync_VendorExists_UpdatesStatusAndBranches()
        {
            // Arrange
            var vendor = new BO.Entities.Vendor { VendorId = 1, IsActive = true };
            var branch = new Branch { BranchId = 1, VendorId = 1, IsActive = true };
            
            _vendorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vendor);
            _branchRepoMock.Setup(r => r.GetAllByVendorIdAsync(1)).ReturnsAsync(new List<Branch> { branch });

            // Act
            var result = await _vendorService.SuspendVendorAsync(1);

            // Assert
            Assert.True(result);
            Assert.False(vendor.IsActive);
            Assert.False(branch.IsActive);
            _vendorRepoMock.Verify(r => r.UpdateAsync(vendor), Times.Once);
            _branchRepoMock.Verify(r => r.UpdateAsync(branch), Times.Once);
        }
    }
}
