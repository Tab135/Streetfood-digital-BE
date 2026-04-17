using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IDishRepository
    {
        Task<Dish> CreateAsync(Dish dish);
        Task AddDishTastesAsync(List<DishTaste> dishTastes);
        Task<Dish?> GetByIdAsync(int dishId);
        Task<(List<Dish> items, int totalCount)> GetDishesAsync(int? vendorId, int? categoryId, string? keyword, int pageNumber, int pageSize);
        Task<(List<Dish> items, int totalCount)> GetDishesByBranchAsync(int branchId, int? categoryId, string? keyword, int pageNumber, int pageSize, bool includeInactive = false);
        Task UpdateAsync(Dish dish);
        Task DeleteAsync(int dishId);
        Task RemoveDishTastesAsync(int dishId);
        Task AddBranchDishAsync(BranchDish branchDish);
        Task RemoveBranchDishAsync(int branchId, int dishId);
        Task<BranchDish?> GetBranchDishAsync(int branchId, int dishId);
        Task UpdateBranchDishStatusAsync(int branchId, int dishId, bool isSoldOut);
    }
}
