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
        Task<List<DietaryPreference>> GetByIdsAsync(List<int> ids);
        Task<bool> IsInUseAsync(int id);
        Task<bool> UpdateIsActiveAsync(int id, bool isActive);
    }
}