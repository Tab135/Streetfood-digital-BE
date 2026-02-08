using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class UserDietaryPreferenceDAO
    {
        private readonly StreetFoodDbContext _context;
        public UserDietaryPreferenceDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        public async Task<List<DietaryPreference>> GetPreferencesByUserId(int userId)
        {
            return await _context.UserDietaryPreferences
                .Where(u => u.UserId == userId)
                .Include(u => u.DietaryPreference)
                .Select(u => u.DietaryPreference)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AssignPreferencesToUser(int userId, List<int> dietaryPreferenceIds)
        {
            // Remove existing preferences for the user
            var existing = _context.UserDietaryPreferences.Where(u => u.UserId == userId);
            _context.UserDietaryPreferences.RemoveRange(existing);

            // Add new preferences
            var newItems = dietaryPreferenceIds.Select(id => new UserDietaryPreference
            {
                UserId = userId,
                DietaryPreferenceId = id
            });

            await _context.UserDietaryPreferences.AddRangeAsync(newItems);
            await _context.SaveChangesAsync();
        }

        public async Task<List<User>> GetUsersWithPreferences()
        {
            return await _context.Users
                .Include(u => u.DietaryPreferences)
                    .ThenInclude(up => up.DietaryPreference)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> UserHasPreference(int userId, int dietaryPreferenceId)
        {
            return await _context.UserDietaryPreferences.AnyAsync(u => u.UserId == userId && u.DietaryPreferenceId == dietaryPreferenceId);
        }
    }
}