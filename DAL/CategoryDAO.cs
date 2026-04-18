using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL
{
    public class CategoryDAO
    {
        private readonly StreetFoodDbContext _context;

        public CategoryDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Category> CreateAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Category?> GetByIdAsync(int categoryId)
        {
            return await _context.Categories.FindAsync(categoryId);
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _context.Categories.ToListAsync();
        }

        public async Task UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }


        public async Task<bool> ExistsByIdAsync(int categoryId)
        {
            return await _context.Categories.AnyAsync(c => c.CategoryId == categoryId);
        }

        public async Task<bool> IsInUseAsync(int id)
        {
            return await _context.Dishes.AnyAsync(x => x.CategoryId == id);
        }

        public async Task<bool> UpdateIsActiveAsync(int categoryId, bool isActive)
        {
           var rowsAffected =  await _context.Categories
                .Where(c => c.CategoryId == categoryId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, isActive));
            
            return rowsAffected > 0;
        }
    }
}
