using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BO.DTO.Taste;
using BO.Entities;
using BO.Exceptions;
using Moq;
using Repository.Interfaces;
using Service;
using Xunit;

namespace StreetFood.Tests.TasteTests
{
    public class TasteServiceTests
    {
        private readonly Mock<ITasteRepository> _tasteRepoMock;
        private readonly Mock<IVendorRepository> _vendorRepoMock;
        private readonly Mock<IBranchRepository> _branchRepoMock;
        private readonly TasteService _tasteService;

        public TasteServiceTests()
        {
            _tasteRepoMock = new Mock<ITasteRepository>();
            _vendorRepoMock = new Mock<IVendorRepository>();
            _branchRepoMock = new Mock<IBranchRepository>();

            _tasteService = new TasteService(
                _tasteRepoMock.Object,
                _vendorRepoMock.Object,
                _branchRepoMock.Object
            );
        }

        // --- SECTION: CREATE TASTE (SV_TASTE_01) ---

        [Fact]
        public async Task CreateTasteAsync_WithNameAndDescription_Success() // 01
        {
            var dto = new CreateTasteDto { Name = "Spicy", Description = "Very hot" };
            _tasteRepoMock.Setup(r => r.CreateAsync(It.IsAny<Taste>()))
                .ReturnsAsync((Taste t) => new Taste { TasteId = 1, Name = t.Name, Description = t.Description, IsActive = true });

            var result = await _tasteService.CreateTasteAsync(dto, 1);

            Assert.Equal("Spicy", result.Name);
            Assert.Equal("Very hot", result.Description);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task CreateTasteAsync_WithNameOnly_Success() // 02
        {
            var dto = new CreateTasteDto { Name = "Sweet", Description = null };
            _tasteRepoMock.Setup(r => r.CreateAsync(It.IsAny<Taste>()))
                .ReturnsAsync((Taste t) => new Taste { TasteId = 2, Name = t.Name, Description = t.Description, IsActive = true });

            var result = await _tasteService.CreateTasteAsync(dto, 1);

            Assert.Equal("Sweet", result.Name);
            Assert.Null(result.Description);
        }

        [Fact]
        public async Task CreateTasteAsync_IsActiveDefaultsTrue() // 03
        {
            var dto = new CreateTasteDto { Name = "Sour" };
            _tasteRepoMock.Setup(r => r.CreateAsync(It.IsAny<Taste>()))
                .ReturnsAsync((Taste t) => new Taste { TasteId = 3, Name = t.Name, IsActive = true });

            var result = await _tasteService.CreateTasteAsync(dto, 1);

            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task CreateTasteAsync_ReturnsCorrectId() // 04
        {
            var dto = new CreateTasteDto { Name = "Bitter" };
            _tasteRepoMock.Setup(r => r.CreateAsync(It.IsAny<Taste>()))
                .ReturnsAsync(new Taste { TasteId = 99, Name = "Bitter", IsActive = true });

            var result = await _tasteService.CreateTasteAsync(dto, 1);

            Assert.Equal(99, result.TasteId);
        }

        [Fact]
        public async Task CreateTasteAsync_MapToDto_AllFieldsPresent() // 05
        {
            var dto = new CreateTasteDto { Name = "Umami", Description = "Savory" };
            _tasteRepoMock.Setup(r => r.CreateAsync(It.IsAny<Taste>()))
                .ReturnsAsync(new Taste { TasteId = 5, Name = "Umami", Description = "Savory", IsActive = true });

            var result = await _tasteService.CreateTasteAsync(dto, 1);

            Assert.Equal(5, result.TasteId);
            Assert.Equal("Umami", result.Name);
            Assert.Equal("Savory", result.Description);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task CreateTasteAsync_VerifyRepoCalledOnce() // 06
        {
            var dto = new CreateTasteDto { Name = "Salty" };
            _tasteRepoMock.Setup(r => r.CreateAsync(It.IsAny<Taste>()))
                .ReturnsAsync(new Taste { TasteId = 6, Name = "Salty" });

            await _tasteService.CreateTasteAsync(dto, 1);

            _tasteRepoMock.Verify(r => r.CreateAsync(It.IsAny<Taste>()), Times.Once);
        }

        // UTCID07: All DTO fields correctly mapped to returned TasteDto
        [Fact]
        public async Task CreateTasteAsync_AllFieldsMappedToDto()
        {
            var dto = new CreateTasteDto { Name = "Tangy", Description = "Sour citrus flavor" };
            _tasteRepoMock.Setup(r => r.CreateAsync(It.IsAny<Taste>()))
                .ReturnsAsync(new Taste { TasteId = 7, Name = "Tangy", Description = "Sour citrus flavor", IsActive = true });

            var result = await _tasteService.CreateTasteAsync(dto, 1);

            Assert.Equal(7, result.TasteId);
            Assert.Equal("Tangy", result.Name);
            Assert.Equal("Sour citrus flavor", result.Description);
            Assert.True(result.IsActive);
        }

        // --- SECTION: UPDATE TASTE (SV_TASTE_02) ---

        [Fact]
        public async Task UpdateTasteAsync_BothFields_Success() // 01
        {
            var tasteId = 1;
            var existing = new Taste { TasteId = tasteId, Name = "Old", Description = "Old Desc" };
            _tasteRepoMock.Setup(r => r.GetByIdAsync(tasteId)).ReturnsAsync(existing);
            
            var result = await _tasteService.UpdateTasteAsync(tasteId, new UpdateTasteDto { Name = "New", Description = "New Desc" }, 1);
            
            Assert.Equal("New", result.Name);
            Assert.Equal("New Desc", result.Description);
        }

        [Fact]
        public async Task UpdateTasteAsync_OnlyName_Success() // 02
        {
            var tasteId = 2;
            var existing = new Taste { TasteId = tasteId, Name = "Old", Description = "Old Desc" };
            _tasteRepoMock.Setup(r => r.GetByIdAsync(tasteId)).ReturnsAsync(existing);
            
            var result = await _tasteService.UpdateTasteAsync(tasteId, new UpdateTasteDto { Name = "New", Description = null }, 1);
            
            Assert.Equal("New", result.Name);
            Assert.Equal("Old Desc", result.Description); // Description intact
        }

        [Fact]
        public async Task UpdateTasteAsync_OnlyDescription_Success() // 03
        {
            var tasteId = 3;
            var existing = new Taste { TasteId = tasteId, Name = "Old", Description = "Old Desc" };
            _tasteRepoMock.Setup(r => r.GetByIdAsync(tasteId)).ReturnsAsync(existing);
            
            var result = await _tasteService.UpdateTasteAsync(tasteId, new UpdateTasteDto { Name = "", Description = "New Desc" }, 1);
            
            Assert.Equal("Old", result.Name); // Name intact
            Assert.Equal("New Desc", result.Description);
        }

        [Fact]
        public async Task UpdateTasteAsync_NoChanges_Success() // 04
        {
            var tasteId = 4;
            var existing = new Taste { TasteId = tasteId, Name = "Old", Description = "Old Desc" };
            _tasteRepoMock.Setup(r => r.GetByIdAsync(tasteId)).ReturnsAsync(existing);
            
            var result = await _tasteService.UpdateTasteAsync(tasteId, new UpdateTasteDto { Name = null, Description = null }, 1);
            
            Assert.Equal("Old", result.Name); 
            Assert.Equal("Old Desc", result.Description);
        }

        [Fact]
        public async Task UpdateTasteAsync_NotFound_ThrowsException() // 05
        {
            _tasteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Taste?)null);
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _tasteService.UpdateTasteAsync(99, new UpdateTasteDto(), 1));
            Assert.Contains("not found", ex.Message);
        }

        [Fact]
        public async Task UpdateTasteAsync_InvalidId_ThrowsException() // 06
        {
             _tasteRepoMock.Setup(r => r.GetByIdAsync(0)).ReturnsAsync((Taste?)null);
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _tasteService.UpdateTasteAsync(0, new UpdateTasteDto(), 1));
            Assert.Contains("not found", ex.Message);
        }

        [Fact]
        public async Task UpdateTasteAsync_VerifyUpdateCalledOnce() // 07
        {
            var tasteId = 5;
            var existing = new Taste { TasteId = tasteId, Name = "Old" };
            _tasteRepoMock.Setup(r => r.GetByIdAsync(tasteId)).ReturnsAsync(existing);
            _tasteRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Taste>())).Returns(Task.CompletedTask);

            await _tasteService.UpdateTasteAsync(tasteId, new UpdateTasteDto { Name = "New" }, 1);

            _tasteRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Taste>()), Times.Once);
        }

        // --- SECTION: DELETE TASTE (SV_TASTE_02) ---

        [Fact]
        public async Task DeleteTasteAsync_ToggleToFalse_Success() // 01
        {
            var tasteId = 1;
            var existing = new Taste { TasteId = tasteId, IsActive = true };

            _tasteRepoMock.Setup(r => r.GetByIdAsync(tasteId)).ReturnsAsync(existing);
            _tasteRepoMock.Setup(r => r.IsInUseAsync(tasteId)).ReturnsAsync(false);
            _tasteRepoMock.Setup(r => r.UpdateIsActiveAsync(tasteId, false)).ReturnsAsync(true);

            var result = await _tasteService.DeleteTasteAsync(tasteId, 1);
            Assert.True(result);
            _tasteRepoMock.Verify(r => r.UpdateIsActiveAsync(tasteId, false), Times.Once);
        }

        [Fact]
        public async Task DeleteTasteAsync_ToggleToTrue_Success() // 02
        {
            var tasteId = 2;
            var existing = new Taste { TasteId = tasteId, IsActive = false };

            _tasteRepoMock.Setup(r => r.GetByIdAsync(tasteId)).ReturnsAsync(existing);
            _tasteRepoMock.Setup(r => r.UpdateIsActiveAsync(tasteId, true)).ReturnsAsync(true);

            var result = await _tasteService.DeleteTasteAsync(tasteId, 1);
            Assert.True(result);
            _tasteRepoMock.Verify(r => r.UpdateIsActiveAsync(tasteId, true), Times.Once);
        }

        [Fact] // 03
        public async Task DeleteTasteAsync_ToggleToFalse_InUse_ThrowsException()
        {
            var tasteId = 3;
            var existing = new Taste { TasteId = tasteId, IsActive = true };

            _tasteRepoMock.Setup(r => r.GetByIdAsync(tasteId)).ReturnsAsync(existing);
            _tasteRepoMock.Setup(r => r.IsInUseAsync(tasteId)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _tasteService.DeleteTasteAsync(tasteId, 1));
            Assert.Contains("vì đang được sử dụng", ex.Message); 
        }

        [Fact]
        public async Task DeleteTasteAsync_NotFound_ThrowsException() // 04
        {
            _tasteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Taste?)null);
            
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _tasteService.DeleteTasteAsync(99, 1));
            Assert.Contains("not found", ex.Message);
        }

        [Fact]
        public async Task DeleteTasteAsync_VerifyIsInUse_NotCalledForInactive() // 05
        {
            var tasteId = 4;
            var existing = new Taste { TasteId = tasteId, IsActive = false };

            _tasteRepoMock.Setup(r => r.GetByIdAsync(tasteId)).ReturnsAsync(existing);

            await _tasteService.DeleteTasteAsync(tasteId, 1);
            _tasteRepoMock.Verify(r => r.IsInUseAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteTasteAsync_InvalidId_ThrowsException() // 06
        {
            _tasteRepoMock.Setup(r => r.GetByIdAsync(0)).ReturnsAsync((Taste?)null);
            
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _tasteService.DeleteTasteAsync(0, 1));
            Assert.Contains("not found", ex.Message);
        }

        [Fact]
        public async Task DeleteTasteAsync_VerifyToggleValue_IsCorrect() // 07
        {
            var tasteId = 5;
            var existing = new Taste { TasteId = tasteId, IsActive = true };
            _tasteRepoMock.Setup(r => r.GetByIdAsync(tasteId)).ReturnsAsync(existing);
            _tasteRepoMock.Setup(r => r.IsInUseAsync(tasteId)).ReturnsAsync(false);
            _tasteRepoMock.Setup(r => r.UpdateIsActiveAsync(tasteId, false)).ReturnsAsync(true);

            await _tasteService.DeleteTasteAsync(tasteId, 1);

            // Active=true should toggle to false, never to true
            _tasteRepoMock.Verify(r => r.UpdateIsActiveAsync(tasteId, false), Times.Once);
            _tasteRepoMock.Verify(r => r.UpdateIsActiveAsync(tasteId, true), Times.Never);
        }
    }
}
