using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface ICategoryRepository
    {
        Task<Category> CreateAsync(Category category);
        Task<Category?> GetByIdAsync(int categoryId);
        Task<List<Category>> GetAllAsync();
        Task UpdateAsync(Category category);
        Task<bool> ExistsByIdAsync(int categoryId);
        Task<bool> IsInUseAsync(int id);
        Task<Category> UpdateIsActiveAsync(int categoryId, bool isActive);
    }
}
