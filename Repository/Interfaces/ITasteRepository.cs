using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface ITasteRepository
    {
        Task<Taste> CreateAsync(Taste taste);
        Task<Taste?> GetByIdAsync(int tasteId);
        Task<List<Taste>> GetAllAsync();
        Task UpdateAsync(Taste taste);
        Task<List<Taste>> GetByIdsAsync(List<int> tasteIds);
        Task<bool> IsInUseAsync(int id);
        Task<bool> UpdateIsActiveAsync(int id, bool isActive);
    }
}
