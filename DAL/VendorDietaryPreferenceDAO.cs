using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class VendorDietaryPreferenceDAO
    {
        private readonly StreetFoodDbContext _context;

        public VendorDietaryPreferenceDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        public async Task<List<DietaryPreference>> GetPreferencesByVendorId(int vendorId)
        {
            return await _context.VendorDietaryPreferences
                .Where(v => v.VendorId == vendorId)
                .Include(v => v.DietaryPreference)
                .Select(v => v.DietaryPreference)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AssignPreferencesToVendor(int vendorId, List<int> dietaryPreferenceIds)
        {
            var existing = _context.VendorDietaryPreferences.Where(v => v.VendorId == vendorId);
            _context.VendorDietaryPreferences.RemoveRange(existing);

            var newItems = dietaryPreferenceIds.Select(id => new VendorDietaryPreference
            {
                VendorId = vendorId,
                DietaryPreferenceId = id
            });

            await _context.VendorDietaryPreferences.AddRangeAsync(newItems);
            await _context.SaveChangesAsync();
        }
    }
}
