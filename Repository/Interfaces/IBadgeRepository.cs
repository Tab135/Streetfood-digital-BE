using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IBadgeRepository
    {
        Task<Badge> Create(Badge badge);
        Task<Badge?> GetById(int badgeId);
        Task<List<Badge>> GetAll();
        Task<Badge> Update(Badge badge);
        Task<bool> Delete(int badgeId);
        Task<bool> Exists(int badgeId);
    }
}
