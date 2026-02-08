using BO.Common;
using BO.DTO.Dish;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IDishService
    {
        Task<DishResponse> CreateDishAsync(CreateDishRequest request, int userId);
        Task<DishResponse> GetDishByIdAsync(int dishId);
        Task<PaginatedResponse<DishResponse>> GetDishesAsync(int? branchId, int? categoryId, string? keyword, int pageNumber, int pageSize);
        Task<DishResponse> UpdateDishAsync(int dishId, UpdateDishRequest request, int userId);
        Task DeleteDishAsync(int dishId, int userId);
    }
}
