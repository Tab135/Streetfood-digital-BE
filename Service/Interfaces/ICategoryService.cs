using BO.DTO.Category;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface ICategoryService
    {
        Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createDto, int userId, string imageUrl);
        Task<CategoryDto> UpdateCategoryAsync(int id, UpdateCategoryDto updateDto, int userId, string? imageUrl);
        Task<bool> DeleteCategoryAsync(int id, int userId);
        Task<List<CategoryDto>> GetAllCategoriesAsync();
        Task<CategoryDto?> GetCategoryByIdAsync(int id);
    }
}
