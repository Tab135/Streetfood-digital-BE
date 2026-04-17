using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL
{
    public class TasteDAO
    {
        private readonly StreetFoodDbContext _context;

        public TasteDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Taste> CreateAsync(Taste taste)
        {
            _context.Tastes.Add(taste);
            await _context.SaveChangesAsync();
            return taste;
        }

        public async Task<Taste?> GetByIdAsync(int tasteId)
        {
            return await _context.Tastes.FindAsync(tasteId);
        }

        public async Task<List<Taste>> GetAllAsync()
        {
            return await _context.Tastes.ToListAsync();
        }

        public async Task UpdateAsync(Taste taste)
        {
            _context.Tastes.Update(taste);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int tasteId)
        {
            var taste = await _context.Tastes.FindAsync(tasteId);
            if (taste != null)
            {
                _context.Tastes.Remove(taste);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsByIdAsync(int tasteId)
        {
            return await _context.Tastes.AnyAsync(t => t.TasteId == tasteId);
        }

        public async Task<List<Taste>> GetByIdsAsync(List<int> tasteIds)
        {
            return await _context.Tastes
                .Where(t => tasteIds.Contains(t.TasteId))
                .ToListAsync();
        }
        public async Task<bool> IsInUseAsync(int id)
        {
            return await _context.DishTastes.AnyAsync(x => x.TasteId == id);
        }    }
}
