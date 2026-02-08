using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IDishRepository
    {
        Task<Dish> CreateAsync(Dish dish);
        Task AddDishTastesAsync(List<DishTaste> dishTastes);
        Task AddDishDietaryPreferencesAsync(List<DishDietaryPreference> dishDietaryPreferences);
        Task<Dish?> GetByIdAsync(int dishId);
        Task<(List<Dish> items, int totalCount)> GetDishesAsync(int? branchId, int? categoryId, string? keyword, int pageNumber, int pageSize);
        Task UpdateAsync(Dish dish);
        Task DeleteAsync(int dishId);
        Task RemoveDishTastesAsync(int dishId);
        Task RemoveDishDietaryPreferencesAsync(int dishId);
    }
}
