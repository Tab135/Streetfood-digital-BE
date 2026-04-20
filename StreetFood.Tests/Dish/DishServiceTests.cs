using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BO.Common;
using BO.DTO.Dish;
using BO.Entities;
using BO.Exceptions;
using Moq;
using Repository.Interfaces;
using Service;
using Xunit;

namespace StreetFood.Tests.Dish
{
    public class DishServiceTests
    {
        private readonly Mock<IDishRepository> _dishRepoMock;
        private readonly Mock<ICategoryRepository> _catRepoMock;
        private readonly Mock<ITasteRepository> _tasteRepoMock;
        private readonly Mock<IBranchRepository> _branchRepoMock;
        private readonly Mock<IVendorRepository> _vendorRepoMock;
        private readonly DishService _dishService;

        public DishServiceTests()
        {
            _dishRepoMock = new Mock<IDishRepository>();
            _catRepoMock = new Mock<ICategoryRepository>();
            _tasteRepoMock = new Mock<ITasteRepository>();
            _branchRepoMock = new Mock<IBranchRepository>();
            _vendorRepoMock = new Mock<IVendorRepository>();

            _dishService = new DishService(
                _dishRepoMock.Object,
                _catRepoMock.Object,
                _tasteRepoMock.Object,
                _branchRepoMock.Object,
                _vendorRepoMock.Object
            );
        }

        private void SetupVendorAuth(int vendorId, int userId, bool authorized)
        {
            var vendor = new BO.Entities.Vendor { VendorId = vendorId, UserId = authorized ? userId : 999 };
            _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId)).ReturnsAsync(vendor);
            _branchRepoMock.Setup(r => r.GetAllByManagerIdAsync(userId)).ReturnsAsync(new List<BO.Entities.Branch>());
        }

        // --- SECTION: CREATE DISH (7 TEST CASES) ---

        // SV_DISH_01 (UTCID01) - Normal Success
        [Fact]
        public async Task CreateDishAsync_Normal_Success_ReturnsResponse()
        {
            var vendorId = 5; var userId = 10;
            var request = new CreateDishRequest { Name = "Spicy Ramen", Price = 30000, CategoryId = 1, TasteIds = new List<int> { 1 } };
            _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId)).ReturnsAsync(new BO.Entities.Vendor { VendorId = vendorId, UserId = userId });
            _catRepoMock.Setup(r => r.ExistsByIdAsync(1)).ReturnsAsync(true);
            _tasteRepoMock.Setup(r => r.GetByIdsAsync(request.TasteIds)).ReturnsAsync(new List<BO.Entities.Taste> { new BO.Entities.Taste { TasteId = 1 } });
            var dish = new BO.Entities.Dish { DishId = 100, Name = "Spicy Ramen" };
            _dishRepoMock.Setup(r => r.CreateAsync(It.IsAny<BO.Entities.Dish>())).ReturnsAsync(dish);
            _dishRepoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(dish);

            var result = await _dishService.CreateDishAsync(vendorId, request, userId, "url");

            Assert.Equal("Spicy Ramen", result.Name);
        }

        // SV_DISH_01 (UTCID02) - Empty Name
        [Fact]
        public async Task CreateDishAsync_EmptyName_ThrowsException()
        {
            var request = new CreateDishRequest { Name = "  ", Price = 1000 };
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.CreateDishAsync(5, request, 10, "url"));
            Assert.Equal("Tên món ăn là bắt buộc", ex.Message);
        }

        // SV_DISH_01 (UTCID03) - Zero Price
        [Fact]
        public async Task CreateDishAsync_InvalidPrice_ThrowsException()
        {
            var request = new CreateDishRequest { Name = "Free Food", Price = 0 };
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.CreateDishAsync(5, request, 10, "url"));
            Assert.Equal("Giá món ăn phải lớn hơn 0", ex.Message);
        }

        // SV_DISH_01 (UTCID04) - Vendor Not Found
        [Fact]
        public async Task CreateDishAsync_VendorNotFound_ThrowsException()
        {
            var request = new CreateDishRequest { Name = "Ramen", Price = 1000 };
            _vendorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((BO.Entities.Vendor?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.CreateDishAsync(99, request, 1, "url"));
            Assert.Contains("Vendor with ID 99 not found", ex.Message);
        }

        // SV_DISH_01 (UTCID05) - Unauthorized
        [Fact]
        public async Task CreateDishAsync_Unauthorized_ThrowsException()
        {
            var vendorId = 5; var userId = 10;
            var request = new CreateDishRequest { Name = "Ramen", Price = 1000 };
            _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId)).ReturnsAsync(new BO.Entities.Vendor { VendorId = vendorId, UserId = 999 });
            _branchRepoMock.Setup(r => r.GetAllByManagerIdAsync(userId)).ReturnsAsync(new List<BO.Entities.Branch>()); // Added this

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.CreateDishAsync(vendorId, request, userId, "url"));
            Assert.Equal("You do not manage this vendor", ex.Message);
        }

        // SV_DISH_01 (UTCID06) - Category Missing
        [Fact]
        public async Task CreateDishAsync_CategoryMissing_ThrowsException()
        {
            var vendorId = 5; var userId = 10;
            var request = new CreateDishRequest { Name = "Ramen", Price = 1000, CategoryId = 99 };
            _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId)).ReturnsAsync(new BO.Entities.Vendor { VendorId = vendorId, UserId = userId });
            _catRepoMock.Setup(r => r.ExistsByIdAsync(99)).ReturnsAsync(false);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.CreateDishAsync(vendorId, request, userId, "url"));
            Assert.Equal("Category with ID 99 not found", ex.Message);
        }

        // SV_DISH_01 (UTCID07) - Taste Missing
        [Fact]
        public async Task CreateDishAsync_TastesMissing_ThrowsException()
        {
            var vendorId = 5; var userId = 10;
            var request = new CreateDishRequest { Name = "Ramen", Price = 1000, CategoryId = 1, TasteIds = new List<int> { 1 } };
            _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId)).ReturnsAsync(new BO.Entities.Vendor { VendorId = vendorId, UserId = userId });
            _catRepoMock.Setup(r => r.ExistsByIdAsync(1)).ReturnsAsync(true);
            _tasteRepoMock.Setup(r => r.GetByIdsAsync(request.TasteIds)).ReturnsAsync(new List<BO.Entities.Taste>()); 

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.CreateDishAsync(vendorId, request, userId, "url"));
            Assert.Contains("Taste IDs not found", ex.Message);
        }

        // --- SECTION: UPDATE DISH (7 TEST CASES) ---

        // SV_DISH_02 (UTCID01) - Full Update Success
        [Fact]
        public async Task UpdateDishAsync_FullUpdate_Success_ReturnsResponse()
        {
            var dishId = 1; var userId = 10; var vendorId = 5;
            var dish = new BO.Entities.Dish { DishId = dishId, VendorId = vendorId, Name = "Old Name" };
            var request = new UpdateDishRequest { Name = "New Name", Price = 50000, CategoryId = 2, TasteIds = new List<int> { 2 } };
            
            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);
            SetupVendorAuth(vendorId, userId, true);
            _catRepoMock.Setup(r => r.ExistsByIdAsync(2)).ReturnsAsync(true);
            _tasteRepoMock.Setup(r => r.GetByIdsAsync(request.TasteIds)).ReturnsAsync(new List<BO.Entities.Taste> { new BO.Entities.Taste { TasteId = 2 } });

            var result = await _dishService.UpdateDishAsync(dishId, request, userId, null);

            Assert.Equal("New Name", result.Name);
            Assert.Equal(50000, result.Price);
            _dishRepoMock.Verify(r => r.UpdateAsync(It.IsAny<BO.Entities.Dish>()), Times.Once);
        }

        // SV_DISH_02 (UTCID02) - Dish Not Found
        [Fact]
        public async Task UpdateDishAsync_DishNotFound_ThrowsException()
        {
            _dishRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((BO.Entities.Dish?)null);
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.UpdateDishAsync(99, new UpdateDishRequest(), 1, null));
            Assert.Contains("not found", ex.Message);
        }

        // SV_DISH_02 (UTCID03) - Unauthorized
        [Fact]
        public async Task UpdateDishAsync_Unauthorized_ThrowsException()
        {
            var dishId = 1; var vendorId = 5; var userId = 10;
            var dish = new BO.Entities.Dish { DishId = dishId, VendorId = vendorId };
            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);
            SetupVendorAuth(vendorId, userId, false);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.UpdateDishAsync(dishId, new UpdateDishRequest(), userId, null));
            Assert.Equal("You do not manage this vendor", ex.Message);
        }

        // SV_DISH_02 (UTCID04) - Category Missing
        [Fact]
        public async Task UpdateDishAsync_CategoryMissing_ThrowsException()
        {
            var dishId = 1; var vendorId = 5; var userId = 10;
            var dish = new BO.Entities.Dish { DishId = dishId, VendorId = vendorId };
            var request = new UpdateDishRequest { CategoryId = 99 };
            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);
            SetupVendorAuth(vendorId, userId, true);
            _catRepoMock.Setup(r => r.ExistsByIdAsync(99)).ReturnsAsync(false);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.UpdateDishAsync(dishId, request, userId, null));
            Assert.Contains("Category with ID 99 not found", ex.Message);
        }

        // SV_DISH_02 (UTCID05) - Tastes Missing
        [Fact]
        public async Task UpdateDishAsync_TastesMissing_ThrowsException()
        {
            var dishId = 1; var vendorId = 5; var userId = 10;
            var dish = new BO.Entities.Dish { DishId = dishId, VendorId = vendorId };
            var request = new UpdateDishRequest { TasteIds = new List<int> { 99 } };
            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);
            SetupVendorAuth(vendorId, userId, true);
            _tasteRepoMock.Setup(r => r.GetByIdsAsync(request.TasteIds)).ReturnsAsync(new List<BO.Entities.Taste>());

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.UpdateDishAsync(dishId, request, userId, null));
            Assert.Contains("Taste IDs not found", ex.Message);
        }

        // SV_DISH_02 (UTCID06) - Partial Update (No Category/Taste)
        [Fact]
        public async Task UpdateDishAsync_PartialUpdate_Success_ReturnsResponse()
        {
            var dishId = 1; var userId = 10; var vendorId = 5;
            var dish = new BO.Entities.Dish { DishId = dishId, VendorId = vendorId, Name = "Old Name", Price = 1000 };
            var request = new UpdateDishRequest { Name = "Partial" }; // Price, Category, Taste left null
            
            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);
            SetupVendorAuth(vendorId, userId, true);

            var result = await _dishService.UpdateDishAsync(dishId, request, userId, null);

            Assert.Equal("Partial", result.Name);
            Assert.Equal(1000, result.Price); // Price remains unchanged
            _dishRepoMock.Verify(r => r.UpdateAsync(It.IsAny<BO.Entities.Dish>()), Times.Once);
            _dishRepoMock.Verify(r => r.RemoveDishTastesAsync(It.IsAny<int>()), Times.Never); // Tastes unaffected
        }

        // SV_DISH_02 (UTCID07) - Clear Tastes (Empty List)
        [Fact]
        public async Task UpdateDishAsync_ClearTastes_Success_ReturnsResponse()
        {
            var dishId = 1; var userId = 10; var vendorId = 5;
            var dish = new BO.Entities.Dish { DishId = dishId, VendorId = vendorId };
            var request = new UpdateDishRequest { TasteIds = new List<int>() }; // Empty list clears tastes
            
            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);
            SetupVendorAuth(vendorId, userId, true);

            await _dishService.UpdateDishAsync(dishId, request, userId, null);

            _dishRepoMock.Verify(r => r.RemoveDishTastesAsync(dishId), Times.Once);
            _dishRepoMock.Verify(r => r.AddDishTastesAsync(It.IsAny<List<BO.Entities.DishTaste>>()), Times.Never);
        }
        // --- SECTION: DISH BRANCH AVAILABILITY (7 TEST CASES) ---

        // SV_DISH_03 (UTCID01) - Update to Sold Out (Success)
        [Fact]
        public async Task UpdateDishAvailabilityAsync_SetSoldOut_Success()
        {
            var dishId = 1; var branchId = 2; var userId = 10; var vendorId = 5;
            var dish = new BO.Entities.Dish { DishId = dishId, VendorId = vendorId };
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = userId };
            var branchDish = new BO.Entities.BranchDish { DishId = dishId, BranchId = branchId };

            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _dishRepoMock.Setup(r => r.GetBranchDishAsync(branchId, dishId)).ReturnsAsync(branchDish);

            await _dishService.UpdateDishAvailabilityAsync(dishId, branchId, true, userId);

            _dishRepoMock.Verify(r => r.UpdateBranchDishStatusAsync(branchId, dishId, true), Times.Once);
        }

        // SV_DISH_03 (UTCID02) - Update to Available (Success)
        [Fact]
        public async Task UpdateDishAvailabilityAsync_SetAvailable_Success()
        {
            var dishId = 1; var branchId = 2; var userId = 10; var vendorId = 5;
            var dish = new BO.Entities.Dish { DishId = dishId, VendorId = vendorId };
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = userId };
            var branchDish = new BO.Entities.BranchDish { DishId = dishId, BranchId = branchId };

            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _dishRepoMock.Setup(r => r.GetBranchDishAsync(branchId, dishId)).ReturnsAsync(branchDish);

            await _dishService.UpdateDishAvailabilityAsync(dishId, branchId, false, userId);

            _dishRepoMock.Verify(r => r.UpdateBranchDishStatusAsync(branchId, dishId, false), Times.Once);
        }

        // SV_DISH_03 (UTCID03) - Dish Not Found
        [Fact]
        public async Task UpdateDishAvailabilityAsync_DishNotFound_ThrowsException()
        {
            _dishRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((BO.Entities.Dish?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => 
                _dishService.UpdateDishAvailabilityAsync(99, 2, true, 10));
            Assert.Contains("Dish with ID 99 not found", ex.Message);
        }

        // SV_DISH_03 (UTCID04) - Branch Not Found
        [Fact]
        public async Task UpdateDishAvailabilityAsync_BranchNotFound_ThrowsException()
        {
            var dishId = 1;
            var dish = new BO.Entities.Dish { DishId = dishId, VendorId = 5 };
            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);
            _branchRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((BO.Entities.Branch?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => 
                _dishService.UpdateDishAvailabilityAsync(dishId, 99, true, 10));
            Assert.Contains("Branch with ID 99 not found", ex.Message);
        }

        // SV_DISH_03 (UTCID05) - Unauthorized (User doesn't manage branch)
        [Fact]
        public async Task UpdateDishAvailabilityAsync_Unauthorized_ThrowsException()
        {
            var dishId = 1; var branchId = 2; var userId = 10; var vendorId = 5;
            var dish = new BO.Entities.Dish { DishId = dishId, VendorId = vendorId };
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = 999 }; // Different manager
            
            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId)).ReturnsAsync(new BO.Entities.Vendor { UserId = 888 }); // Different owner

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => 
                _dishService.UpdateDishAvailabilityAsync(dishId, branchId, true, userId));
            Assert.Equal("You do not manage this branch", ex.Message);
        }

        // SV_DISH_03 (UTCID06) - Mismatched Vendor
        [Fact]
        public async Task UpdateDishAvailabilityAsync_VendorMismatch_ThrowsException()
        {
            var dishId = 1; var branchId = 2; var userId = 10;
            var dish = new BO.Entities.Dish { DishId = dishId, VendorId = 5 }; // Vendor 5
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = 99, ManagerId = userId }; // Vendor 99

            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => 
                _dishService.UpdateDishAvailabilityAsync(dishId, branchId, true, userId));
            Assert.Equal("Branch does not belong to the same vendor as this dish", ex.Message);
        }

        // SV_DISH_03 (UTCID07) - Dish Not Assigned To Branch
        [Fact]
        public async Task UpdateDishAvailabilityAsync_DishNotAssigned_ThrowsException()
        {
            var dishId = 1; var branchId = 2; var userId = 10; var vendorId = 5;
            var dish = new BO.Entities.Dish { DishId = dishId, VendorId = vendorId };
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = userId };

            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _dishRepoMock.Setup(r => r.GetBranchDishAsync(branchId, dishId)).ReturnsAsync((BO.Entities.BranchDish?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => 
                _dishService.UpdateDishAvailabilityAsync(dishId, branchId, true, userId));
            Assert.Equal("Dish is not assigned to this branch", ex.Message);
        }
        // --- SECTION: ADD DISHES TO BRANCH (7 TEST CASES) ---

        // SV_DISH_04 (UTCID01) - Success (Assign new dish)
        [Fact]
        public async Task AddDishesToBranchAsync_NewDish_Success()
        {
            var branchId = 2; var userId = 10; var vendorId = 5; var dishId = 1;
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = userId };
            var dish = new BO.Entities.Dish { DishId = dishId, VendorId = vendorId };
            
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);
            _dishRepoMock.Setup(r => r.GetBranchDishAsync(branchId, dishId)).ReturnsAsync((BO.Entities.BranchDish?)null);

            await _dishService.AddDishesToBranchAsync(new List<int> { dishId }, branchId, userId);

            _dishRepoMock.Verify(r => r.AddBranchDishAsync(It.Is<BO.Entities.BranchDish>(bd => bd.DishId == dishId && bd.BranchId == branchId)), Times.Once);
        }

        // SV_DISH_04 (UTCID02) - Success (Existing Sold Out -> Become Available)
        [Fact]
        public async Task AddDishesToBranchAsync_ExistingSoldOut_UpdatesToAvailable()
        {
            var branchId = 2; var userId = 10; var vendorId = 5; var dishId = 1;
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = userId };
            var dish = new BO.Entities.Dish { DishId = dishId, VendorId = vendorId };
            var existing = new BO.Entities.BranchDish { BranchId = branchId, DishId = dishId, IsSoldOut = true };
            
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);
            _dishRepoMock.Setup(r => r.GetBranchDishAsync(branchId, dishId)).ReturnsAsync(existing);

            await _dishService.AddDishesToBranchAsync(new List<int> { dishId }, branchId, userId);

            _dishRepoMock.Verify(r => r.UpdateBranchDishStatusAsync(branchId, dishId, false), Times.Once);
        }

        // SV_DISH_04 (UTCID03) - Success (Existing Available -> Do Nothing)
        [Fact]
        public async Task AddDishesToBranchAsync_ExistingAvailable_DoesNothing()
        {
            var branchId = 2; var userId = 10; var vendorId = 5; var dishId = 1;
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = userId };
            var dish = new BO.Entities.Dish { DishId = dishId, VendorId = vendorId };
            var existing = new BO.Entities.BranchDish { BranchId = branchId, DishId = dishId, IsSoldOut = false };
            
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);
            _dishRepoMock.Setup(r => r.GetBranchDishAsync(branchId, dishId)).ReturnsAsync(existing);

            await _dishService.AddDishesToBranchAsync(new List<int> { dishId }, branchId, userId);

            _dishRepoMock.Verify(r => r.UpdateBranchDishStatusAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
            _dishRepoMock.Verify(r => r.AddBranchDishAsync(It.IsAny<BO.Entities.BranchDish>()), Times.Never);
        }

        // SV_DISH_04 (UTCID04) - Branch Not Found
        [Fact]
        public async Task AddDishesToBranchAsync_BranchNotFound_ThrowsException()
        {
            _branchRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((BO.Entities.Branch?)null);
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.AddDishesToBranchAsync(new List<int> { 1 }, 99, 10));
            Assert.Contains("Branch with ID 99 not found", ex.Message);
        }

        // SV_DISH_04 (UTCID05) - Unauthorized
        [Fact]
        public async Task AddDishesToBranchAsync_Unauthorized_ThrowsException()
        {
            var branchId = 2; var userId = 10;
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = 5, ManagerId = 999 };
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new BO.Entities.Vendor { UserId = 888 });

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.AddDishesToBranchAsync(new List<int> { 1 }, branchId, userId));
            Assert.Equal("You do not manage this branch", ex.Message);
        }

        // SV_DISH_04 (UTCID06) - Dish Not Found
        [Fact]
        public async Task AddDishesToBranchAsync_DishNotFound_ThrowsException()
        {
            var branchId = 2; var userId = 10;
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = 5, ManagerId = userId };
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _dishRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((BO.Entities.Dish?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.AddDishesToBranchAsync(new List<int> { 1 }, branchId, userId));
            Assert.Contains("Dish with ID 1 not found", ex.Message);
        }

        // SV_DISH_04 (UTCID07) - Vendor Mismatch
        [Fact]
        public async Task AddDishesToBranchAsync_VendorMismatch_ThrowsException()
        {
            var branchId = 2; var userId = 10;
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = 5, ManagerId = userId };
            var dish = new BO.Entities.Dish { DishId = 1, VendorId = 99 }; // Different vendor
            
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _dishRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dish);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.AddDishesToBranchAsync(new List<int> { 1 }, branchId, userId));
            Assert.Contains("does not belong to your vendor", ex.Message);
        }
        // --- SECTION: REMOVE DISHES FROM BRANCH (7 TEST CASES) ---

        // SV_DISH_05 (UTCID01) - Success (Remove one)
        [Fact]
        public async Task RemoveDishesFromBranchAsync_OneDish_Success()
        {
            var branchId = 2; var userId = 10; var vendorId = 5; var dishId = 1;
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = userId };
            var dish = new BO.Entities.Dish { DishId = dishId, VendorId = vendorId };
            
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(dish);

            await _dishService.RemoveDishesFromBranchAsync(new List<int> { dishId }, branchId, userId);

            _dishRepoMock.Verify(r => r.RemoveBranchDishAsync(branchId, dishId), Times.Once);
        }

        // SV_DISH_05 (UTCID02) - Success (Remove multiple)
        [Fact]
        public async Task RemoveDishesFromBranchAsync_MultipleDishes_Success()
        {
            var branchId = 2; var userId = 10; var vendorId = 5;
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = userId };
            var dish1 = new BO.Entities.Dish { DishId = 1, VendorId = vendorId };
            var dish2 = new BO.Entities.Dish { DishId = 2, VendorId = vendorId };
            
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _dishRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dish1);
            _dishRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(dish2);

            await _dishService.RemoveDishesFromBranchAsync(new List<int> { 1, 2 }, branchId, userId);

            _dishRepoMock.Verify(r => r.RemoveBranchDishAsync(branchId, 1), Times.Once);
            _dishRepoMock.Verify(r => r.RemoveBranchDishAsync(branchId, 2), Times.Once);
        }

        // SV_DISH_05 (UTCID03) - Branch Not Found
        [Fact]
        public async Task RemoveDishesFromBranchAsync_BranchNotFound_ThrowsException()
        {
            _branchRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((BO.Entities.Branch?)null);
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.RemoveDishesFromBranchAsync(new List<int> { 1 }, 99, 10));
            Assert.Contains("Branch with ID 99 not found", ex.Message);
        }

        // SV_DISH_05 (UTCID04) - Unauthorized
        [Fact]
        public async Task RemoveDishesFromBranchAsync_Unauthorized_ThrowsException()
        {
            var branchId = 2; var userId = 10;
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = 5, ManagerId = 999 };
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new BO.Entities.Vendor { UserId = 888 });

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.RemoveDishesFromBranchAsync(new List<int> { 1 }, branchId, userId));
            Assert.Equal("You do not manage this branch", ex.Message);
        }

        // SV_DISH_05 (UTCID05) - Dish Not Found
        [Fact]
        public async Task RemoveDishesFromBranchAsync_DishNotFound_ThrowsException()
        {
            var branchId = 2; var userId = 10;
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = 5, ManagerId = userId };
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _dishRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((BO.Entities.Dish?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.RemoveDishesFromBranchAsync(new List<int> { 1 }, branchId, userId));
            Assert.Contains("Dish with ID 1 not found", ex.Message);
        }

        // SV_DISH_05 (UTCID06) - Vendor Mismatch
        [Fact]
        public async Task RemoveDishesFromBranchAsync_VendorMismatch_ThrowsException()
        {
            var branchId = 2; var userId = 10;
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = 5, ManagerId = userId };
            var dish = new BO.Entities.Dish { DishId = 1, VendorId = 99 }; // Different vendor
            
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _dishRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dish);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _dishService.RemoveDishesFromBranchAsync(new List<int> { 1 }, branchId, userId));
            Assert.Contains("does not belong to your vendor", ex.Message);
        }

        // SV_DISH_05 (UTCID07) - Boundary (Empty List)
        [Fact]
        public async Task RemoveDishesFromBranchAsync_EmptyList_DoesNothing()
        {
            var branchId = 2; var userId = 10;
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = 5, ManagerId = userId };
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);

            await _dishService.RemoveDishesFromBranchAsync(new List<int>(), branchId, userId);

            _dishRepoMock.Verify(r => r.RemoveBranchDishAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }
    }
}
