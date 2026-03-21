using BO.Entities;
using DAL;
using Repository.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class TierRepository : ITierRepository
    {
        private readonly TierDAO _tierDAO;

        public TierRepository(TierDAO tierDAO)
        {
            _tierDAO = tierDAO;
        }

        public async Task<List<Tier>> GetAllAsync()
        {
            return await _tierDAO.GetAllAsync();
        }

        public async Task<Tier?> GetByIdAsync(int tierId)
        {
            return await _tierDAO.GetByIdAsync(tierId);
        }
    }
}