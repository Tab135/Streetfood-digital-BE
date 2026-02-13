using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IDietaryPreferenceRepository
    {
        Task<List<DietaryPreference>> GetAll();
        Task<DietaryPreference?> GetById(int id);
        Task<DietaryPreference> Create(DietaryPreference dietaryPreference);
        Task<DietaryPreference> Update(DietaryPreference dietaryPreference);
        Task<bool> Delete(int id);
        Task<bool> Exists(int id);
        Task<List<DietaryPreference>> GetByIdsAsync(List<int> ids);
    }
}