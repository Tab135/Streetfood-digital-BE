using BO.Entities;
using DAL;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class TasteRepository : ITasteRepository
    {
        private readonly TasteDAO _tasteDAO;

        public TasteRepository(TasteDAO tasteDAO)
        {
            _tasteDAO = tasteDAO ?? throw new ArgumentNullException(nameof(tasteDAO));
        }

        public async Task<Taste> CreateAsync(Taste taste)
        {
            return await _tasteDAO.CreateAsync(taste);
        }

        public async Task<Taste?> GetByIdAsync(int tasteId)
        {
            return await _tasteDAO.GetByIdAsync(tasteId);
        }

        public async Task<List<Taste>> GetAllAsync()
        {
            return await _tasteDAO.GetAllAsync();
        }

        public async Task UpdateAsync(Taste taste)
        {
            await _tasteDAO.UpdateAsync(taste);
        }

        public async Task<List<Taste>> GetByIdsAsync(List<int> tasteIds)
        {
            return await _tasteDAO.GetByIdsAsync(tasteIds);
        }

        public async Task<bool> IsInUseAsync(int id)
        {
            return await _tasteDAO.IsInUseAsync(id);
        }

        public async Task<bool> UpdateIsActiveAsync(int id, bool isActive)
        {
            return await _tasteDAO.UpdateIsActiveAsync(id, isActive);
        }
    }
}
