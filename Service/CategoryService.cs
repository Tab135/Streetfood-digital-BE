using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BO.DTO.Category;
using BO.Entities;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;
        private readonly IVendorRepository _vendorRepository;
        private readonly IBranchRepository _branchRepository;

        public CategoryService(
            ICategoryRepository repo,
            IVendorRepository vendorRepository,
            IBranchRepository branchRepository)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _vendorRepository = vendorRepository ?? throw new ArgumentNullException(nameof(vendorRepository));
            _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
        }

        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createDto, int userId, string imageUrl)
        {
            var entity = new Category
            {
                Name = createDto.Name,
                Description = createDto.Description,
                ImageUrl = imageUrl
            };

            var created = await _repo.CreateAsync(entity);
            return MapToDto(created);
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
        {
            var category = await _repo.GetByIdAsync(id);
            return category == null ? null : MapToDto(category);
        }

        public async Task<List<CategoryDto>> GetAllCategoriesAsync()
        {
            var list = await _repo.GetAllAsync();
            return list.Select(MapToDto).ToList();
        }

        public async Task<CategoryDto> UpdateCategoryAsync(int id, UpdateCategoryDto updateDto, int userId, string? imageUrl)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new DomainExceptions($"Category with id {id} not found");

            if (!string.IsNullOrEmpty(updateDto.Name))
                existing.Name = updateDto.Name;

            if (updateDto.Description != null)
                existing.Description = updateDto.Description;

            if (!string.IsNullOrWhiteSpace(imageUrl))
                existing.ImageUrl = imageUrl;

            await _repo.UpdateAsync(existing);
            return MapToDto(existing);
        }

        public async Task<bool> DeleteCategoryAsync(int id, int userId)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new DomainExceptions($"Category with id {id} not found");

            if (existing.IsActive)
            {
                var isInUse = await _repo.IsInUseAsync(id);
                if (isInUse)
                    throw new DomainExceptions($"Không thể vô hiệu hóa danh mục này vì đang được sử dụng");
            }

            var newStatus = !existing.IsActive;
            await _repo.UpdateIsActiveAsync(id, newStatus);
            return true;
        }

        private async Task ValidateVendorHasVerifiedBranchAsync(int userId)
        {
            var vendor = await _vendorRepository.GetByUserIdAsync(userId);
            if (vendor == null)
            {
                throw new DomainExceptions("You must be a vendor to manage categories");
            }

            var branches = await _branchRepository.GetAllByVendorIdAsync(vendor.VendorId);
            if (!branches.Any(b => b.IsVerified))
            {
                throw new DomainExceptions("You must have at least one verified branch to manage categories");
            }
        }

        private static CategoryDto MapToDto(Category c)
        {
            return new CategoryDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                IsActive = c.IsActive
            };
        }
    }
}
