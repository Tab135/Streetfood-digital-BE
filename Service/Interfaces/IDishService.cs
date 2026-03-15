using BO.Common;
using BO.DTO.Dish;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IDishService
    {
        Task<DishResponse> CreateDishAsync(int vendorId, CreateDishRequest request, int userId);
        Task<DishResponse> GetDishByIdAsync(int dishId);
        Task<PaginatedResponse<DishResponse>> GetDishesByVendorAsync(int vendorId, int? categoryId, string? keyword, int pageNumber, int pageSize);
        Task<PaginatedResponse<DishResponse>> GetDishesByBranchAsync(int branchId, int? categoryId, string? keyword, int pageNumber, int pageSize);
        Task<DishResponse> UpdateDishAsync(int dishId, UpdateDishRequest request, int userId);
        Task DeleteDishAsync(int dishId, int userId);
        Task AddDishToBranchAsync(int dishId, int branchId, int userId);
        Task RemoveDishFromBranchAsync(int dishId, int branchId, int userId);
        Task UpdateDishAvailabilityAsync(int dishId, int branchId, bool isAvailable, int userId);
    }
}
