using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class DishDAO
    {
        private readonly StreetFoodDbContext _context;

        public DishDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Dish> CreateAsync(Dish dish)
        {
            _context.Dishes.Add(dish);
            await _context.SaveChangesAsync();
            return dish;
        }

        public async Task AddDishTastesAsync(List<DishTaste> dishTastes)
        {
            _context.DishTastes.AddRange(dishTastes);
            await _context.SaveChangesAsync();
        }

        public async Task<Dish?> GetByIdAsync(int dishId)
        {
            return await _context.Dishes
                .Include(d => d.BranchDishes)
                .Include(d => d.Vendor)
                .Include(d => d.Category)
                .Include(d => d.DishTastes)
                    .ThenInclude(dt => dt.Taste)
                .FirstOrDefaultAsync(d => d.DishId == dishId);
        }

        public async Task<(List<Dish> items, int totalCount)> GetDishesAsync(
            int? vendorId,
            int? categoryId,
            string? keyword,
            int pageNumber,
            int pageSize)
        {
            var query = _context.Dishes
                .Include(d => d.BranchDishes)
                .Include(d => d.Vendor)
                .Include(d => d.Category)
                .Include(d => d.DishTastes)
                    .ThenInclude(dt => dt.Taste)
                .AsQueryable();

            if (vendorId.HasValue)
            {
                query = query.Where(d => d.VendorId == vendorId.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(d => d.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var lowerKeyword = keyword.ToLower();
                query = query.Where(d => d.Name.ToLower().Contains(lowerKeyword)
                                      || (d.Description != null && d.Description.ToLower().Contains(lowerKeyword)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<Dish> items, int totalCount)> GetDishesByBranchAsync(
            int branchId,
            int? categoryId,
            string? keyword,
            int pageNumber,
            int pageSize,
            bool includeInactive = false)
        {
            var query = _context.Dishes
                .Where(d => d.BranchDishes.Any(bd => bd.BranchId == branchId))
                .Include(d => d.BranchDishes)
                .Include(d => d.Vendor)
                .Include(d => d.Category)
                .Include(d => d.DishTastes)
                    .ThenInclude(dt => dt.Taste)
                .AsQueryable();

            if (!includeInactive)
                query = query.Where(d => d.IsActive);

            if (categoryId.HasValue)
                query = query.Where(d => d.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var lower = keyword.ToLower();
                query = query.Where(d => d.Name.ToLower().Contains(lower)
                    || (d.Description != null && d.Description.ToLower().Contains(lower)));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddBranchDishAsync(BranchDish branchDish)
        {
            _context.BranchDishes.Add(branchDish);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveBranchDishAsync(int branchId, int dishId)
        {
            var bd = await _context.BranchDishes
                .FirstOrDefaultAsync(x => x.BranchId == branchId && x.DishId == dishId);
            if (bd != null)
            {
                _context.BranchDishes.Remove(bd);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<BranchDish?> GetBranchDishAsync(int branchId, int dishId)
        {
            return await _context.BranchDishes
                .FirstOrDefaultAsync(x => x.BranchId == branchId && x.DishId == dishId);
        }

        public async Task UpdateBranchDishStatusAsync(int branchId, int dishId, bool isSoldOut)
        {
            var branchDish = await _context.BranchDishes
                .FirstOrDefaultAsync(x => x.BranchId == branchId && x.DishId == dishId);

            if (branchDish != null)
            {
                branchDish.IsSoldOut = isSoldOut;
                branchDish.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateAsync(Dish dish)
        {
            dish.UpdatedAt = DateTime.UtcNow;
            _context.Dishes.Update(dish);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int dishId)
        {
            var dish = await _context.Dishes.FindAsync(dishId);
            if (dish != null)
            {
                _context.Dishes.Remove(dish);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveDishTastesAsync(int dishId)
        {
            var dishTastes = await _context.DishTastes
                .Where(dt => dt.DishId == dishId)
                .ToListAsync();
            _context.DishTastes.RemoveRange(dishTastes);
            await _context.SaveChangesAsync();
        }

    }
}
