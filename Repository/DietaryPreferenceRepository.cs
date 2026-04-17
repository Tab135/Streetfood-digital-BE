using BO.Entities;
using DAL;
using Repository.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class DietaryPreferenceRepository : IDietaryPreferenceRepository
    {
        private readonly DietaryPreferenceDAO _dao;
        public DietaryPreferenceRepository(DietaryPreferenceDAO dao)
        {
            _dao = dao;
        }

        public async Task<DietaryPreference> Create(DietaryPreference dietaryPreference)
        {
            return await _dao.Create(dietaryPreference);
        }

        public async Task<bool> Delete(int id)
        {
            return await _dao.Delete(id);
        }

        public async Task<bool> Exists(int id)
        {
            return await _dao.Exists(id);
        }

        public async Task<List<DietaryPreference>> GetAll()
        {
            return await _dao.GetAll();
        }

        public async Task<DietaryPreference?> GetById(int id)
        {
            return await _dao.GetById(id);
        }

        public async Task<DietaryPreference> Update(DietaryPreference dietaryPreference)
        {
            return await _dao.Update(dietaryPreference);
        }

        public async Task<List<DietaryPreference>> GetByIdsAsync(List<int> ids)
        {
            return await _dao.GetByIdsAsync(ids);
        }
        public async Task<bool> IsInUseAsync(int id)
        {
            return await _dao.IsInUseAsync(id);
        }    }
}