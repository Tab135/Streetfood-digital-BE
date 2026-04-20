using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BO.DTO.Category;
using BO.Entities;
using BO.Exceptions;
using Moq;
using Repository.Interfaces;
using Service;
using Xunit;

namespace StreetFood.Tests.CategoryTests
{
    public class CategoryServiceTests
    {
        private readonly Mock<ICategoryRepository> _categoryRepoMock;
        private readonly Mock<IVendorRepository> _vendorRepoMock;
        private readonly Mock<IBranchRepository> _branchRepoMock;
        private readonly CategoryService _categoryService;

        public CategoryServiceTests()
        {
            _categoryRepoMock = new Mock<ICategoryRepository>();
            _vendorRepoMock = new Mock<IVendorRepository>();
            _branchRepoMock = new Mock<IBranchRepository>();

            _categoryService = new CategoryService(
                _categoryRepoMock.Object,
                _vendorRepoMock.Object,
                _branchRepoMock.Object
            );
        }

        // --- SECTION: CREATE CATEGORY (SV_CATE_01) ---

        // UTCID01: Success with Name, Description, and Image URL
        [Fact]
        public async Task CreateCategoryAsync_WithAllFields_Success()
        {
            var dto = new CreateCategoryDto { Name = "Drinks", Description = "Cold beverages" };
            var imageUrl = "https://cdn.example.com/drinks.jpg";

            _categoryRepoMock.Setup(r => r.CreateAsync(It.IsAny<Category>()))
                .ReturnsAsync((Category c) => new Category
                {
                    CategoryId = 1, Name = c.Name, Description = c.Description, ImageUrl = c.ImageUrl, IsActive = true
                });

            var result = await _categoryService.CreateCategoryAsync(dto, 1, imageUrl);

            Assert.Equal("Drinks", result.Name);
            Assert.Equal("Cold beverages", result.Description);
            Assert.Equal(imageUrl, result.ImageUrl);
            Assert.True(result.IsActive);
        }

        // UTCID02: Success with Name only, no Description
        [Fact]
        public async Task CreateCategoryAsync_WithNameOnly_Success()
        {
            var dto = new CreateCategoryDto { Name = "Desserts", Description = null };

            _categoryRepoMock.Setup(r => r.CreateAsync(It.IsAny<Category>()))
                .ReturnsAsync((Category c) => new Category { CategoryId = 2, Name = c.Name, Description = null, IsActive = true });

            var result = await _categoryService.CreateCategoryAsync(dto, 1, "img.jpg");

            Assert.Equal("Desserts", result.Name);
            Assert.Null(result.Description);
        }

        // UTCID03: IsActive defaults to true on creation
        [Fact]
        public async Task CreateCategoryAsync_IsActiveDefaultsTrue()
        {
            var dto = new CreateCategoryDto { Name = "Snacks" };

            _categoryRepoMock.Setup(r => r.CreateAsync(It.IsAny<Category>()))
                .ReturnsAsync(new Category { CategoryId = 3, Name = "Snacks", IsActive = true });

            var result = await _categoryService.CreateCategoryAsync(dto, 1, "");

            Assert.True(result.IsActive);
        }

        // UTCID04: Image URL is correctly passed to entity
        [Fact]
        public async Task CreateCategoryAsync_ImageUrl_CorrectlyMapped()
        {
            var dto = new CreateCategoryDto { Name = "Meals" };
            var imageUrl = "https://cdn.example.com/meals.png";

            _categoryRepoMock.Setup(r => r.CreateAsync(It.IsAny<Category>()))
                .ReturnsAsync((Category c) => new Category { CategoryId = 4, Name = c.Name, ImageUrl = c.ImageUrl });

            var result = await _categoryService.CreateCategoryAsync(dto, 1, imageUrl);

            Assert.Equal(imageUrl, result.ImageUrl);
        }

        // UTCID05: Returns correct CategoryId from repo
        [Fact]
        public async Task CreateCategoryAsync_ReturnsCorrectId()
        {
            var dto = new CreateCategoryDto { Name = "Noodles" };

            _categoryRepoMock.Setup(r => r.CreateAsync(It.IsAny<Category>()))
                .ReturnsAsync(new Category { CategoryId = 99, Name = "Noodles", IsActive = true });

            var result = await _categoryService.CreateCategoryAsync(dto, 1, "img.jpg");

            Assert.Equal(99, result.CategoryId);
        }

        // UTCID06: Verify CreateAsync is called exactly once
        [Fact]
        public async Task CreateCategoryAsync_VerifyRepoCalledOnce()
        {
            var dto = new CreateCategoryDto { Name = "Soups" };

            _categoryRepoMock.Setup(r => r.CreateAsync(It.IsAny<Category>()))
                .ReturnsAsync(new Category { CategoryId = 6, Name = "Soups" });

            await _categoryService.CreateCategoryAsync(dto, 1, "img.jpg");

            _categoryRepoMock.Verify(r => r.CreateAsync(It.IsAny<Category>()), Times.Once);
        }

        // UTCID07: All DTO fields are correctly mapped into returned DTO
        [Fact]
        public async Task CreateCategoryAsync_AllFieldsMappedToDto()
        {
            var dto = new CreateCategoryDto { Name = "Rice", Description = "Fried rice dishes" };
            var imageUrl = "https://cdn.example.com/rice.jpg";

            _categoryRepoMock.Setup(r => r.CreateAsync(It.IsAny<Category>()))
                .ReturnsAsync(new Category { CategoryId = 7, Name = "Rice", Description = "Fried rice dishes", ImageUrl = imageUrl, IsActive = true });

            var result = await _categoryService.CreateCategoryAsync(dto, 1, imageUrl);

            Assert.Equal(7, result.CategoryId);
            Assert.Equal("Rice", result.Name);
            Assert.Equal("Fried rice dishes", result.Description);
            Assert.Equal(imageUrl, result.ImageUrl);
            Assert.True(result.IsActive);
        }

        // --- SECTION: UPDATE CATEGORY (SV_CATE_02) ---

        // UTCID01: Success updating all fields
        [Fact]
        public async Task UpdateCategoryAsync_AllFields_Success()
        {
            var id = 1;
            var existing = new Category { CategoryId = id, Name = "Old", Description = "Old Desc", ImageUrl = "old.jpg" };
            _categoryRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);

            var result = await _categoryService.UpdateCategoryAsync(id,
                new UpdateCategoryDto { Name = "New", Description = "New Desc" }, 1, "new.jpg");

            Assert.Equal("New", result.Name);
            Assert.Equal("New Desc", result.Description);
            Assert.Equal("new.jpg", result.ImageUrl);
        }

        // UTCID02: Update Name only, Description and Image unchanged
        [Fact]
        public async Task UpdateCategoryAsync_NameOnly_Success()
        {
            var id = 2;
            var existing = new Category { CategoryId = id, Name = "Old", Description = "Keep Desc", ImageUrl = "keep.jpg" };
            _categoryRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);

            var result = await _categoryService.UpdateCategoryAsync(id,
                new UpdateCategoryDto { Name = "New Name", Description = null }, 1, null);

            Assert.Equal("New Name", result.Name);
            Assert.Equal("Keep Desc", result.Description);
            Assert.Equal("keep.jpg", result.ImageUrl);
        }

        // UTCID03: Update Image URL only, Name and Description unchanged
        [Fact]
        public async Task UpdateCategoryAsync_ImageOnly_Success()
        {
            var id = 3;
            var existing = new Category { CategoryId = id, Name = "Keep", Description = "Keep Desc", ImageUrl = "old.jpg" };
            _categoryRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);

            var result = await _categoryService.UpdateCategoryAsync(id,
                new UpdateCategoryDto { Name = null, Description = null }, 1, "new.jpg");

            Assert.Equal("Keep", result.Name);
            Assert.Equal("new.jpg", result.ImageUrl);
        }

        // UTCID04: No actual changes - all fields null/empty, entity unchanged
        [Fact]
        public async Task UpdateCategoryAsync_NoChanges_EntityUnchanged()
        {
            var id = 4;
            var existing = new Category { CategoryId = id, Name = "Keep", Description = "Keep Desc", ImageUrl = "keep.jpg" };
            _categoryRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);

            var result = await _categoryService.UpdateCategoryAsync(id,
                new UpdateCategoryDto { Name = null, Description = null }, 1, null);

            Assert.Equal("Keep", result.Name);
            Assert.Equal("Keep Desc", result.Description);
            Assert.Equal("keep.jpg", result.ImageUrl);
        }

        // UTCID05: Category not found - throws DomainException
        [Fact]
        public async Task UpdateCategoryAsync_NotFound_ThrowsException()
        {
            _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Category?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() =>
                _categoryService.UpdateCategoryAsync(99, new UpdateCategoryDto(), 1, null));

            Assert.Contains("not found", ex.Message);
        }

        // UTCID06: ID = 0 (invalid) - not found throws exception
        [Fact]
        public async Task UpdateCategoryAsync_InvalidId_ThrowsException()
        {
            _categoryRepoMock.Setup(r => r.GetByIdAsync(0)).ReturnsAsync((Category?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() =>
                _categoryService.UpdateCategoryAsync(0, new UpdateCategoryDto(), 1, null));

            Assert.Contains("not found", ex.Message);
        }

        // UTCID07: Verify UpdateAsync is called exactly once on valid update
        [Fact]
        public async Task UpdateCategoryAsync_VerifyUpdateCalledOnce()
        {
            var id = 5;
            var existing = new Category { CategoryId = id, Name = "Old" };
            _categoryRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _categoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);

            await _categoryService.UpdateCategoryAsync(id, new UpdateCategoryDto { Name = "New" }, 1, null);

            _categoryRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Once);
        }

        // --- SECTION: DELETE CATEGORY (SV_CATE_03) ---

        // UTCID01: Toggle active→inactive, not in use
        [Fact]
        public async Task DeleteCategoryAsync_ToggleToFalse_NotInUse_Success()
        {
            var id = 1;
            var existing = new Category { CategoryId = id, IsActive = true };
            _categoryRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _categoryRepoMock.Setup(r => r.IsInUseAsync(id)).ReturnsAsync(false);
            _categoryRepoMock.Setup(r => r.UpdateIsActiveAsync(id, false)).ReturnsAsync(true);

            var result = await _categoryService.DeleteCategoryAsync(id, 1);

            Assert.True(result);
            _categoryRepoMock.Verify(r => r.UpdateIsActiveAsync(id, false), Times.Once);
        }

        // UTCID02: Toggle inactive→active, IsInUse check skipped
        [Fact]
        public async Task DeleteCategoryAsync_ToggleToTrue_Success()
        {
            var id = 2;
            var existing = new Category { CategoryId = id, IsActive = false };
            _categoryRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _categoryRepoMock.Setup(r => r.UpdateIsActiveAsync(id, true)).ReturnsAsync(true);

            var result = await _categoryService.DeleteCategoryAsync(id, 1);

            Assert.True(result);
            _categoryRepoMock.Verify(r => r.IsInUseAsync(It.IsAny<int>()), Times.Never);
            _categoryRepoMock.Verify(r => r.UpdateIsActiveAsync(id, true), Times.Once);
        }

        // UTCID03: Toggle active→inactive, category in use - throws exception
        [Fact]
        public async Task DeleteCategoryAsync_ToggleToFalse_InUse_ThrowsException()
        {
            var id = 3;
            var existing = new Category { CategoryId = id, IsActive = true };
            _categoryRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _categoryRepoMock.Setup(r => r.IsInUseAsync(id)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() =>
                _categoryService.DeleteCategoryAsync(id, 1));

            Assert.Contains("vì đang được sử dụng", ex.Message);
        }

        // UTCID04: Category not found - throws exception
        [Fact]
        public async Task DeleteCategoryAsync_NotFound_ThrowsException()
        {
            _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Category?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() =>
                _categoryService.DeleteCategoryAsync(99, 1));

            Assert.Contains("not found", ex.Message);
        }

        // UTCID05: Inactive category - IsInUse check is never called
        [Fact]
        public async Task DeleteCategoryAsync_Inactive_IsInUseNeverCalled()
        {
            var id = 4;
            var existing = new Category { CategoryId = id, IsActive = false };
            _categoryRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _categoryRepoMock.Setup(r => r.UpdateIsActiveAsync(id, true)).ReturnsAsync(true);

            await _categoryService.DeleteCategoryAsync(id, 1);

            _categoryRepoMock.Verify(r => r.IsInUseAsync(It.IsAny<int>()), Times.Never);
        }

        // UTCID06: ID = 0 (invalid) - not found throws exception
        [Fact]
        public async Task DeleteCategoryAsync_InvalidId_ThrowsException()
        {
            _categoryRepoMock.Setup(r => r.GetByIdAsync(0)).ReturnsAsync((Category?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() =>
                _categoryService.DeleteCategoryAsync(0, 1));

            Assert.Contains("not found", ex.Message);
        }

        // UTCID07: Verify UpdateIsActiveAsync is called with correct new boolean value
        [Fact]
        public async Task DeleteCategoryAsync_VerifyToggleValue_IsCorrect()
        {
            var id = 5;
            var existing = new Category { CategoryId = id, IsActive = true };
            _categoryRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
            _categoryRepoMock.Setup(r => r.IsInUseAsync(id)).ReturnsAsync(false);
            _categoryRepoMock.Setup(r => r.UpdateIsActiveAsync(id, false)).ReturnsAsync(true);

            await _categoryService.DeleteCategoryAsync(id, 1);

            // Active=true should toggle to false, never to true
            _categoryRepoMock.Verify(r => r.UpdateIsActiveAsync(id, false), Times.Once);
            _categoryRepoMock.Verify(r => r.UpdateIsActiveAsync(id, true), Times.Never);
        }
    }
}
