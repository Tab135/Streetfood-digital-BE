using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface ITierRepository
    {
        Task<List<Tier>> GetAllAsync();
        Task<Tier?> GetByIdAsync(int tierId);
    }
}