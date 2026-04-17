using BO.Entities;
using DAL;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly CategoryDAO _categoryDAO;

        public CategoryRepository(CategoryDAO categoryDAO)
        {
            _categoryDAO = categoryDAO ?? throw new ArgumentNullException(nameof(categoryDAO));
        }

        public async Task<Category> CreateAsync(Category category)
        {
            return await _categoryDAO.CreateAsync(category);
        }

        public async Task<Category?> GetByIdAsync(int categoryId)
        {
            return await _categoryDAO.GetByIdAsync(categoryId);
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _categoryDAO.GetAllAsync();
        }

        public async Task UpdateAsync(Category category)
        {
            await _categoryDAO.UpdateAsync(category);
        }

        public async Task DeleteAsync(int categoryId)
        {
            await _categoryDAO.DeleteAsync(categoryId);
        }

        public async Task<bool> ExistsByIdAsync(int categoryId)
        {
            return await _categoryDAO.ExistsByIdAsync(categoryId);
        }

        public async Task<bool> IsInUseAsync(int id)
        {
            return await _categoryDAO.IsInUseAsync(id);
        }
    }
}
