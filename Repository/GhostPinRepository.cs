using BO.Entities;
using DAL;
using Repository.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class GhostPinRepository : IGhostPinRepository
    {
        private readonly GhostPinDAO _dao;

        public GhostPinRepository(GhostPinDAO dao)
        {
            _dao = dao;
        }

        public async Task<GhostPin> CreateAsync(GhostPin pin) => await _dao.CreateGhostPinAsync(pin);
        public async Task<GhostPin> GetByIdAsync(int id) => await _dao.GetGhostPinByIdAsync(id);
        public async Task<List<GhostPin>> GetAllAsync() => await _dao.GetAllGhostPinsAsync();
        public async Task UpdateAsync(GhostPin pin) => await _dao.UpdateGhostPinAsync(pin);
        public async Task DeleteAsync(int id) => await _dao.DeleteGhostPinAsync(id);
    }
}
