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

        public async Task DeleteAsync(int tasteId)
        {
            await _tasteDAO.DeleteAsync(tasteId);
        }

        public async Task<bool> ExistsByIdAsync(int tasteId)
        {
            return await _tasteDAO.ExistsByIdAsync(tasteId);
        }

        public async Task<List<Taste>> GetByIdsAsync(List<int> tasteIds)
        {
            return await _tasteDAO.GetByIdsAsync(tasteIds);
        }
    }
}
