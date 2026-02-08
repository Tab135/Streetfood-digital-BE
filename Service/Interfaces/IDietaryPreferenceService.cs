using BO.DTO.Dietary;
using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IDietaryPreferenceService
    {
        Task<DietaryPreferenceDto> CreateDietaryPreference(CreateDietaryPreferenceDto createDto);
        Task<DietaryPreferenceDto> UpdateDietaryPreference(int id, UpdateDietaryPreferenceDto updateDto);
        Task<bool> DeleteDietaryPreference(int id);
        Task<List<DietaryPreferenceDto>> GetAllDietaryPreferences();
        Task<DietaryPreferenceDto?> GetDietaryPreferenceById(int id);
    }
}