using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace DAL
{
    public class GhostPinDAO
    {
        private readonly StreetFoodDbContext _context;

        public GhostPinDAO(StreetFoodDbContext context)
        {
            _context = context;
        }

        public async Task<GhostPin> CreateGhostPinAsync(GhostPin pin)
        {
            await _context.GhostPins.AddAsync(pin);
            await _context.SaveChangesAsync();
            return pin;
        }

        public async Task<GhostPin> GetGhostPinByIdAsync(int id)
        {
            return await _context.GhostPins
                .Include(g => g.Creator)
                .Include(g => g.LinkedBranch)
                .FirstOrDefaultAsync(g => g.GhostPinId == id);
        }

        public async Task<List<GhostPin>> GetAllGhostPinsAsync()
        {
            return await _context.GhostPins
                .Include(g => g.Creator)
                .Include(g => g.LinkedBranch)
                .ToListAsync();
        }

        public async Task UpdateGhostPinAsync(GhostPin pin)
        {
            _context.GhostPins.Update(pin);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteGhostPinAsync(int id)
        {
            var pin = await GetGhostPinByIdAsync(id);
            if (pin != null)
            {
                _context.GhostPins.Remove(pin);
                await _context.SaveChangesAsync();
            }
        }
    }
}
