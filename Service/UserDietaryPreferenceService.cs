using BO.DTO.Dietary;
using BO.DTO.Users;
using Repository.Interfaces;
using Service.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service
{
    public class UserDietaryPreferenceService : IUserDietaryPreferenceService
    {
        private readonly IUserDietaryPreferenceRepository _userDietaryRepo;
        private readonly IDietaryPreferenceRepository _dietaryRepo;

        public UserDietaryPreferenceService(IUserDietaryPreferenceRepository userDietaryRepo, IDietaryPreferenceRepository dietaryRepo)
        {
            _userDietaryRepo = userDietaryRepo;
            _dietaryRepo = dietaryRepo;
        }

        public async Task AssignPreferencesToUser(int userId, List<int> dietaryPreferenceIds)
        {
            // Validate dietary preference ids
            var allPrefs = await _dietaryRepo.GetAll();
            var validIds = allPrefs.Select(p => p.DietaryPreferenceId).ToHashSet();

            if (dietaryPreferenceIds.Any(id => !validIds.Contains(id)))
                throw new System.Exception("One or more dietary preference ids are invalid");

            await _userDietaryRepo.AssignPreferencesToUser(userId, dietaryPreferenceIds);
        }

        public async Task<List<DietaryPreferenceDto>> GetPreferencesByUserId(int userId)
        {
            var preferences = await _userDietaryRepo.GetPreferencesByUserId(userId);
            return preferences.Select(p => new DietaryPreferenceDto
            {
                DietaryPreferenceId = p.DietaryPreferenceId,
                Name = p.Name,
                Description = p.Description
            }).ToList();
        }

        public async Task<List<UserDietaryPreferencesDto>> GetAllUsersWithPreferences()
        {
            var users = await _userDietaryRepo.GetUsersWithPreferences();
            var result = users.Select(u => new UserDietaryPreferencesDto
            {
                UserId = u.Id,
                UserName = u.UserName,
                DietaryPreferences = u.DietaryPreferences?.Select(dp => dp.DietaryPreference.Name).ToList() ?? new List<string>()
            }).ToList();

            return result;
        }
    }
}