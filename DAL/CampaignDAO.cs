using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class CampaignDAO
    {
        private readonly StreetFoodDbContext _context;

        public CampaignDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Campaign> CreateAsync(Campaign campaign)
        {
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();
            return campaign;
        }

        public async Task<Campaign?> GetByIdAsync(int id)
        {
            return await _context.Campaigns.FirstOrDefaultAsync(c => c.CampaignId == id);
        }

        public async Task<List<Campaign>> GetAllSystemActiveAsync()
        {
            return await _context.Campaigns
                .Where(c => c.CreatedByBranchId == null && c.EndDate >= DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<List<Campaign>> GetByBranchIdAsync(int branchId)
        {
            return await _context.Campaigns
                .Where(c => c.CreatedByBranchId == branchId)
                .ToListAsync();
        }

        public async Task UpdateAsync(Campaign campaign)
        {
            _context.Campaigns.Update(campaign);
            await _context.SaveChangesAsync();
        }
    }
}
