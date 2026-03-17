using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IGhostPinRepository
    {
        Task<GhostPin> CreateAsync(GhostPin pin);
        Task<GhostPin> GetByIdAsync(int id);
        Task<List<GhostPin>> GetAllAsync();
        Task UpdateAsync(GhostPin pin);
        Task DeleteAsync(int id);
    }
}
