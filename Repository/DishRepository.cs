using BO.Entities;
using DAL;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class DishRepository : IDishRepository
    {
        private readonly DishDAO _dishDAO;

        public DishRepository(DishDAO dishDAO)
        {
            _dishDAO = dishDAO ?? throw new ArgumentNullException(nameof(dishDAO));
        }

        public async Task<Dish> CreateAsync(Dish dish)
        {
            return await _dishDAO.CreateAsync(dish);
        }

        public async Task AddDishTastesAsync(List<DishTaste> dishTastes)
        {
            await _dishDAO.AddDishTastesAsync(dishTastes);
        }

        public async Task AddDishDietaryPreferencesAsync(List<DishDietaryPreference> dishDietaryPreferences)
        {
            await _dishDAO.AddDishDietaryPreferencesAsync(dishDietaryPreferences);
        }

        public async Task<Dish?> GetByIdAsync(int dishId)
        {
            return await _dishDAO.GetByIdAsync(dishId);
        }

        public async Task<(List<Dish> items, int totalCount)> GetDishesAsync(
            int? vendorId,
            int? categoryId,
            string? keyword,
            int pageNumber,
            int pageSize)
        {
            return await _dishDAO.GetDishesAsync(vendorId, categoryId, keyword, pageNumber, pageSize);
        }

        public async Task UpdateAsync(Dish dish)
        {
            await _dishDAO.UpdateAsync(dish);
        }

        public async Task DeleteAsync(int dishId)
        {
            await _dishDAO.DeleteAsync(dishId);
        }

        public async Task RemoveDishTastesAsync(int dishId)
        {
            await _dishDAO.RemoveDishTastesAsync(dishId);
        }

        public async Task RemoveDishDietaryPreferencesAsync(int dishId)
        {
            await _dishDAO.RemoveDishDietaryPreferencesAsync(dishId);
        }

        public async Task<(List<Dish> items, int totalCount)> GetDishesByBranchAsync(int branchId, int? categoryId, string? keyword, int pageNumber, int pageSize)
        {
            return await _dishDAO.GetDishesByBranchAsync(branchId, categoryId, keyword, pageNumber, pageSize);
        }

        public async Task AddBranchDishAsync(BranchDish branchDish)
        {
            await _dishDAO.AddBranchDishAsync(branchDish);
        }

        public async Task RemoveBranchDishAsync(int branchId, int dishId)
        {
            await _dishDAO.RemoveBranchDishAsync(branchId, dishId);
        }

        public async Task<BranchDish?> GetBranchDishAsync(int branchId, int dishId)
        {
            return await _dishDAO.GetBranchDishAsync(branchId, dishId);
        }

        public async Task UpdateBranchDishAvailabilityAsync(int branchId, int dishId, bool isAvailable)
        {
            await _dishDAO.UpdateBranchDishAvailabilityAsync(branchId, dishId, isAvailable);
        }
    }
}
