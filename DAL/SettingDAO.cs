using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL
{
    public class SettingDAO
    {
        private readonly StreetFoodDbContext _context;

        public SettingDAO(StreetFoodDbContext context)
        {
            _context = context;
        }

        public async Task<List<Setting>> GetAllAsync()
        {
            return await _context.Settings.ToListAsync();
        }

        public async Task<Setting?> GetByNameAsync(string name)
        {
            return await _context.Settings
                .FirstOrDefaultAsync(s => s.Name == name);
        }

        public async Task<Setting?> GetByIdAsync(int id)
        {
            return await _context.Settings.FindAsync(id);
        }

        public async Task UpdateAsync(Setting setting)
        {
            _context.Settings.Update(setting);
            await _context.SaveChangesAsync();
        }
    }
}