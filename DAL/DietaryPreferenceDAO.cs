using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class DietaryPreferenceDAO
    {
        private readonly StreetFoodDbContext _context;
        public DietaryPreferenceDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        public async Task<List<DietaryPreference>> GetAll()
        {
            return await _context.DietaryPreferences.AsNoTracking().ToListAsync();
        }

        public async Task<DietaryPreference?> GetById(int id)
        {
            return await _context.DietaryPreferences.FirstOrDefaultAsync(x => x.DietaryPreferenceId == id);
        }

        public async Task<List<DietaryPreference>> GetByIdsAsync(List<int> ids)
        {
            return await _context.DietaryPreferences
                .Where(dp => ids.Contains(dp.DietaryPreferenceId))
                .ToListAsync();
        }

        public async Task<DietaryPreference> Create(DietaryPreference dietaryPreference)
        {
            dietaryPreference.DietaryPreferenceId = 0;
            _context.DietaryPreferences.Add(dietaryPreference);
            await _context.SaveChangesAsync();
            return dietaryPreference;
        }

        public async Task<DietaryPreference> Update(DietaryPreference dietaryPreference)
        {
            _context.DietaryPreferences.Update(dietaryPreference);
            await _context.SaveChangesAsync();
            return dietaryPreference;
        }

        public async Task<bool> Delete(int id)
        {
            var existing = await _context.DietaryPreferences.FirstOrDefaultAsync(x => x.DietaryPreferenceId == id);
            if (existing == null) return false;
            _context.DietaryPreferences.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Exists(int id)
        {
            return await _context.DietaryPreferences.AnyAsync(x => x.DietaryPreferenceId == id);
        }

        public async Task<bool> IsInUseAsync(int id)
        {
            var usedByUser = await _context.UserDietaryPreferences.AnyAsync(x => x.DietaryPreferenceId == id);
            if (usedByUser) return true;

            var usedByDish = await _context.DishDietaryPreferences.AnyAsync(x => x.DietaryPreferenceId == id);
            if (usedByDish) return true;

            var usedByVendor = await _context.VendorDietaryPreferences.AnyAsync(x => x.DietaryPreferenceId == id);
            return usedByVendor;
        }
    }
}