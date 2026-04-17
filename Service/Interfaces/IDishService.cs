using BO.Common;
using BO.DTO.Dish;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IDishService
    {
        Task<DishResponse> CreateDishAsync(int vendorId, CreateDishRequest request, int userId, string imageUrl);
        Task<DishResponse> GetDishByIdAsync(int dishId);
        Task<PaginatedResponse<DishResponse>> GetDishesByVendorAsync(int vendorId, int? categoryId, string? keyword, int pageNumber, int pageSize);
        Task<PaginatedResponse<DishResponse>> GetDishesByBranchAsync(int branchId, int? categoryId, string? keyword, int pageNumber, int pageSize, int? currentUserId = null);
        Task<DishResponse> UpdateDishAsync(int dishId, UpdateDishRequest request, int userId, string? imageUrl);
        Task DeleteDishAsync(int dishId, int userId);
        Task AddDishesToBranchAsync(List<int> dishIds, int branchId, int userId);
        Task RemoveDishesFromBranchAsync(List<int> dishIds, int branchId, int userId);
        Task UpdateDishAvailabilityAsync(int dishId, int branchId, bool isSoldOut, int userId);
    }
}
