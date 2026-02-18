using BO.Common;
using BO.DTO.Users;
using BO.DTO.Dietary;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IUserDietaryPreferenceService
    {
        Task AssignPreferencesToUser(int userId, List<int> dietaryPreferenceIds);
        Task<List<DietaryPreferenceDto>> GetPreferencesByUserId(int userId);
        Task<PaginatedResponse<UserDietaryPreferencesDto>> GetAllUsersWithPreferences(int pageNumber, int pageSize);
    }
}