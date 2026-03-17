using BO.Common;
using BO.DTO.Dish;
using BO.Entities;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service
{
    public class DishService : IDishService
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ITasteRepository _tasteRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly IDietaryPreferenceRepository _dietaryPreferenceRepository;

        public DishService(
            IDishRepository dishRepository,
            ICategoryRepository categoryRepository,
            ITasteRepository tasteRepository,
            IBranchRepository branchRepository,
            IVendorRepository vendorRepository,
            IDietaryPreferenceRepository dietaryPreferenceRepository)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _tasteRepository = tasteRepository ?? throw new ArgumentNullException(nameof(tasteRepository));
            _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
            _vendorRepository = vendorRepository ?? throw new ArgumentNullException(nameof(vendorRepository));
            _dietaryPreferenceRepository = dietaryPreferenceRepository ?? throw new ArgumentNullException(nameof(dietaryPreferenceRepository));
        }

        public async Task<DishResponse> CreateDishAsync(int vendorId, CreateDishRequest request, int userId, string imageUrl)
        {
            // Validate vendor exists and user owns it
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new DomainExceptions($"Vendor with ID {vendorId} not found");
            }
            if (vendor.UserId != userId)
            {
                throw new DomainExceptions("You do not own this vendor");
            }

            // TODO: (Optional) Get category instance instead of checking existence, to avoid multiple DB calls. Same for Taste and DietaryPreference.
            // Validate Category exists
            var categoryExists = await _categoryRepository.ExistsByIdAsync(request.CategoryId);
            if (!categoryExists)
            {
                throw new DomainExceptions($"Category with ID {request.CategoryId} not found");
            }

            // TODO: Taste and DietaryPreference is required in CreateDishRequest
            // Validate all TasteIds exist
            if (request.TasteIds != null && request.TasteIds.Count > 0)
            {
                var existingTastes = await _tasteRepository.GetByIdsAsync(request.TasteIds);
                var missingTasteIds = request.TasteIds.Except(existingTastes.Select(t => t.TasteId)).ToList();
                if (missingTasteIds.Any())
                {
                    throw new DomainExceptions($"Taste IDs not found: {string.Join(", ", missingTasteIds)}");
                }
            }

            // Validate all DietaryPreferenceIds exist
            if (request.DietaryPreferenceIds != null && request.DietaryPreferenceIds.Count > 0)
            {
                var existingPrefs = await _dietaryPreferenceRepository.GetByIdsAsync(request.DietaryPreferenceIds);
                var missingPrefIds = request.DietaryPreferenceIds.Except(existingPrefs.Select(p => p.DietaryPreferenceId)).ToList();
                if (missingPrefIds.Any())
                {
                    throw new DomainExceptions($"DietaryPreference IDs not found: {string.Join(", ", missingPrefIds)}");
                }
            }

            // Step 1: Create Dish -> SaveChanges (to generate DishId)
            var dish = new Dish
            {
                Name = request.Name,
                Price = request.Price,
                Description = request.Description,
                ImageUrl = imageUrl,
                IsActive = request.IsActive,
                VendorId = vendorId,
                CategoryId = request.CategoryId
            };

            var createdDish = await _dishRepository.CreateAsync(dish);

            // Step 2: Create DishTaste rows
            if (request.TasteIds != null && request.TasteIds.Count > 0)
            {
                var dishTastes = request.TasteIds.Select(tasteId => new DishTaste
                {
                    DishId = createdDish.DishId,
                    TasteId = tasteId
                }).ToList();

                await _dishRepository.AddDishTastesAsync(dishTastes);
            }

            // Step 3: Create DishDietaryPreference rows
            if (request.DietaryPreferenceIds != null && request.DietaryPreferenceIds.Count > 0)
            {
                var dishDietaryPreferences = request.DietaryPreferenceIds.Select(prefId => new DishDietaryPreference
                {
                    DishId = createdDish.DishId,
                    DietaryPreferenceId = prefId
                }).ToList();

                await _dishRepository.AddDishDietaryPreferencesAsync(dishDietaryPreferences);
            }

            // Reload dish with full navigation data
            var fullDish = await _dishRepository.GetByIdAsync(createdDish.DishId);
            return MapToResponse(fullDish!);
        }

        public async Task<DishResponse> GetDishByIdAsync(int dishId)
        {
            var dish = await _dishRepository.GetByIdAsync(dishId);
            if (dish == null)
            {
                throw new DomainExceptions($"Dish with ID {dishId} not found");
            }

            return MapToResponse(dish);
        }

        public async Task<PaginatedResponse<DishResponse>> GetDishesByVendorAsync(
            int vendorId,
            int? categoryId,
            string? keyword,
            int pageNumber,
            int pageSize)
        {
            var (dishes, totalCount) = await _dishRepository.GetDishesAsync(vendorId, categoryId, keyword, pageNumber, pageSize);
            var items = dishes.Select(MapToResponse).ToList();
            return new PaginatedResponse<DishResponse>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PaginatedResponse<DishResponse>> GetDishesByBranchAsync(
            int branchId,
            int? categoryId,
            string? keyword,
            int pageNumber,
            int pageSize)
        {
            var (dishes, totalCount) = await _dishRepository.GetDishesByBranchAsync(branchId, categoryId, keyword, pageNumber, pageSize);
            var items = dishes.Select(d => MapToResponseForBranch(d, branchId)).ToList();
            return new PaginatedResponse<DishResponse>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<DishResponse> UpdateDishAsync(int dishId, UpdateDishRequest request, int userId, string? imageUrl)
        {
            // Get existing dish
            var dish = await _dishRepository.GetByIdAsync(dishId);
            if (dish == null)
            {
                throw new DomainExceptions($"Dish with ID {dishId} not found");
            }

            // Validate user owns the vendor
            var vendor = await _vendorRepository.GetByIdAsync(dish.VendorId);
            if (vendor == null || vendor.UserId != userId)
            {
                throw new DomainExceptions("You do not own this vendor");
            }

            // Validate CategoryId if provided
            if (request.CategoryId.HasValue)
            {
                var categoryExists = await _categoryRepository.ExistsByIdAsync(request.CategoryId.Value);
                if (!categoryExists)
                {
                    throw new DomainExceptions($"Category with ID {request.CategoryId.Value} not found");
                }
            }

            // Update only non-null fields
            if (!string.IsNullOrEmpty(request.Name))
                dish.Name = request.Name;

            if (request.Price.HasValue)
                dish.Price = request.Price.Value;

            if (request.Description != null)
                dish.Description = request.Description;

            if (!string.IsNullOrWhiteSpace(imageUrl))
                dish.ImageUrl = imageUrl;

            if (request.IsActive.HasValue)
                dish.IsActive = request.IsActive.Value;

            if (request.CategoryId.HasValue)
                dish.CategoryId = request.CategoryId.Value;

            await _dishRepository.UpdateAsync(dish);

            // Update TasteIds if provided (replace all)
            if (request.TasteIds != null)
            {
                // Validate TasteIds
                if (request.TasteIds.Count > 0)
                {
                    var existingTastes = await _tasteRepository.GetByIdsAsync(request.TasteIds);
                    var missingTasteIds = request.TasteIds.Except(existingTastes.Select(t => t.TasteId)).ToList();
                    if (missingTasteIds.Any())
                    {
                        throw new DomainExceptions($"Taste IDs not found: {string.Join(", ", missingTasteIds)}");
                    }
                }

                // Remove existing then add new
                await _dishRepository.RemoveDishTastesAsync(dishId);

                if (request.TasteIds.Count > 0)
                {
                    var dishTastes = request.TasteIds.Select(tasteId => new DishTaste
                    {
                        DishId = dishId,
                        TasteId = tasteId
                    }).ToList();

                    await _dishRepository.AddDishTastesAsync(dishTastes);
                }
            }

            // Update DietaryPreferenceIds if provided (replace all)
            if (request.DietaryPreferenceIds != null)
            {
                // Validate DietaryPreferenceIds
                if (request.DietaryPreferenceIds.Count > 0)
                {
                    var existingPrefs = await _dietaryPreferenceRepository.GetByIdsAsync(request.DietaryPreferenceIds);
                    var missingPrefIds = request.DietaryPreferenceIds.Except(existingPrefs.Select(p => p.DietaryPreferenceId)).ToList();
                    if (missingPrefIds.Any())
                    {
                        throw new DomainExceptions($"DietaryPreference IDs not found: {string.Join(", ", missingPrefIds)}");
                    }
                }

                // Remove existing then add new
                await _dishRepository.RemoveDishDietaryPreferencesAsync(dishId);

                if (request.DietaryPreferenceIds.Count > 0)
                {
                    var dishDietaryPreferences = request.DietaryPreferenceIds.Select(prefId => new DishDietaryPreference
                    {
                        DishId = dishId,
                        DietaryPreferenceId = prefId
                    }).ToList();

                    await _dishRepository.AddDishDietaryPreferencesAsync(dishDietaryPreferences);
                }
            }

            // Reload full dish
            var fullDish = await _dishRepository.GetByIdAsync(dishId);
            return MapToResponse(fullDish!);
        }

        public async Task DeleteDishAsync(int dishId, int userId)
        {
            var dish = await _dishRepository.GetByIdAsync(dishId);
            if (dish == null)
            {
                throw new DomainExceptions($"Dish with ID {dishId} not found");
            }

            // Validate user owns the vendor
            var vendor = await _vendorRepository.GetByIdAsync(dish.VendorId);
            if (vendor == null || vendor.UserId != userId)
            {
                throw new DomainExceptions("You do not own this vendor");
            }

            await _dishRepository.DeleteAsync(dishId);
        }

        public async Task AddDishesToBranchAsync(List<int> dishIds, int branchId, int userId)
        {
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
                throw new DomainExceptions($"Branch with ID {branchId} not found");

            var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId);
            if (vendor == null || vendor.UserId != userId)
                throw new DomainExceptions("You do not own this branch");

            foreach (var dishId in dishIds)
            {
                var dish = await _dishRepository.GetByIdAsync(dishId);
                if (dish == null)
                    throw new DomainExceptions($"Dish with ID {dishId} not found");
                if (dish.VendorId != branch.VendorId)
                    throw new DomainExceptions($"Dish with ID {dishId} does not belong to your vendor");

                var existing = await _dishRepository.GetBranchDishAsync(branchId, dishId);
                if (existing != null)
                {
                    if (existing.IsSoldOut)
                    {
                        await _dishRepository.UpdateBranchDishStatusAsync(branchId, dishId, false);
                    }
                    continue;
                }

                await _dishRepository.AddBranchDishAsync(new BranchDish
                {
                    BranchId = branchId,
                    DishId = dishId,
                    IsSoldOut = false
                });
            }
        }

        public async Task RemoveDishesFromBranchAsync(List<int> dishIds, int branchId, int userId)
        {
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
                throw new DomainExceptions($"Branch with ID {branchId} not found");

            var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId);
            if (vendor == null || vendor.UserId != userId)
                throw new DomainExceptions("You do not own this branch");

            foreach (var dishId in dishIds)
            {
                var dish = await _dishRepository.GetByIdAsync(dishId);
                if (dish == null || dish.VendorId != branch.VendorId)
                    continue;

                await _dishRepository.RemoveBranchDishAsync(branchId, dishId);
            }
        }

        public async Task UpdateDishAvailabilityAsync(int dishId, int branchId, bool isSoldOut, int userId)
        {
            var dish = await _dishRepository.GetByIdAsync(dishId);
            if (dish == null)
                throw new DomainExceptions($"Dish with ID {dishId} not found");

            var vendor = await _vendorRepository.GetByIdAsync(dish.VendorId);
            if (vendor == null || vendor.UserId != userId)
                throw new DomainExceptions("You do not own this vendor");

            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
                throw new DomainExceptions($"Branch with ID {branchId} not found");

            if (branch.VendorId != dish.VendorId)
                throw new DomainExceptions("Branch does not belong to the same vendor as this dish");

            var branchDish = await _dishRepository.GetBranchDishAsync(branchId, dishId);
            if (branchDish == null)
                throw new DomainExceptions("Dish is not assigned to this branch");

            await _dishRepository.UpdateBranchDishStatusAsync(branchId, dishId, isSoldOut);
        }

        private static DishResponse MapToResponseForBranch(Dish dish, int branchId)
        {
            var response = MapToResponse(dish);
            var branchDish = dish.BranchDishes?.FirstOrDefault(bd => bd.BranchId == branchId);
            if (branchDish != null)
            {
                response.IsSoldOut = branchDish.IsSoldOut;
            }

            return response;
        }

        private static DishResponse MapToResponse(Dish dish)
        {
            return new DishResponse
            {
                DishId = dish.DishId,
                Name = dish.Name,
                Price = dish.Price,
                Description = dish.Description,
                ImageUrl = dish.ImageUrl,
                IsSoldOut = dish.IsSoldOut,
                IsActive = dish.IsActive,
                CreatedAt = dish.CreatedAt,
                UpdatedAt = dish.UpdatedAt,
                VendorId = dish.VendorId,
                CategoryId = dish.CategoryId,
                CategoryName = dish.Category?.Name ?? string.Empty,
                TasteNames = dish.DishTastes?
                    .Where(dt => dt.Taste != null)
                    .Select(dt => dt.Taste.Name)
                    .ToList() ?? new List<string>(),
                DietaryPreferenceNames = dish.DishDietaryPreferences?
                    .Where(ddp => ddp.DietaryPreference != null)
                    .Select(ddp => ddp.DietaryPreference.Name)
                    .ToList() ?? new List<string>()
            };
        }
    }
}
