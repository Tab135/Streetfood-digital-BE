using BO.Entities;
using DAL;
using Repository.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class SettingRepository : ISettingRepository
    {
        private readonly SettingDAO _dao;

        public SettingRepository(SettingDAO dao)
        {
            _dao = dao;
        }

        public Task<List<Setting>> GetAllAsync() => _dao.GetAllAsync();

        public Task<Setting?> GetByNameAsync(string name) => _dao.GetByNameAsync(name);

        public Task<Setting?> GetByIdAsync(int id) => _dao.GetByIdAsync(id);

        public Task UpdateAsync(Setting setting) => _dao.UpdateAsync(setting);
    }
}
