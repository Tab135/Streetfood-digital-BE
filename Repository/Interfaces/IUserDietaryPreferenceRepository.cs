using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IUserDietaryPreferenceRepository
    {
        Task<List<DietaryPreference>> GetPreferencesByUserId(int userId);
        Task AssignPreferencesToUser(int userId, List<int> dietaryPreferenceIds);
        Task<(List<User> items, int totalCount)> GetUsersWithPreferences(int pageNumber, int pageSize);
        Task<bool> UserHasPreference(int userId, int dietaryPreferenceId);
    }
}