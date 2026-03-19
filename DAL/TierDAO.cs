using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL
{
    public class TierDAO
    {
        private readonly StreetFoodDbContext _context;

        public TierDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Tier?> GetByIdAsync(int tierId)
        {
            return await _context.Tiers.FindAsync(tierId);
        }

        public async Task<List<Tier>> GetAllAsync()
        {
            return await _context.Tiers.ToListAsync();
        }

        // Normally we don't need Create/Update/Delete for static Tiers, 
        // but adding it here for completeness of DAO pattern.
        public async Task<Tier> CreateAsync(Tier tier)
        {
            _context.Tiers.Add(tier);
            await _context.SaveChangesAsync();
            return tier;
        }

        public async Task UpdateAsync(Tier tier)
        {
            _context.Tiers.Update(tier);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int tierId)
        {
            var tier = await _context.Tiers.FindAsync(tierId);     
            if (tier != null)
            {
                _context.Tiers.Remove(tier);
                await _context.SaveChangesAsync();
            }
        }
    }
}