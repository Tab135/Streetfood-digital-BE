using BO.Entities;
using DAL;
using Repository.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class UserDietaryPreferenceRepository : IUserDietaryPreferenceRepository
    {
        private readonly UserDietaryPreferenceDAO _dao;
        public UserDietaryPreferenceRepository(UserDietaryPreferenceDAO dao)
        {
            _dao = dao;
        }

        public async Task AssignPreferencesToUser(int userId, List<int> dietaryPreferenceIds)
        {
            await _dao.AssignPreferencesToUser(userId, dietaryPreferenceIds);
        }

        public async Task<List<DietaryPreference>> GetPreferencesByUserId(int userId)
        {
            return await _dao.GetPreferencesByUserId(userId);
        }

        public async Task<List<User>> GetUsersWithPreferences()
        {
            return await _dao.GetUsersWithPreferences();
        }

        public async Task<bool> UserHasPreference(int userId, int dietaryPreferenceId)
        {
            return await _dao.UserHasPreference(userId, dietaryPreferenceId);
        }
    }
}