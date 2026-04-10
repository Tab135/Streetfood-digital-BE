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
    }
}