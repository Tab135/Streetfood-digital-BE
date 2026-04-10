using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface ISettingRepository
    {
        Task<List<Setting>> GetAllAsync();
        Task<Setting?> GetByNameAsync(string name);
        Task UpdateAsync(Setting setting);
    }
}