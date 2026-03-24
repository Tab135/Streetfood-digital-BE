using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class BranchCampaignDAO
    {
        private readonly StreetFoodDbContext _context;

        public BranchCampaignDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<BranchCampaign> CreateAsync(BranchCampaign branchCampaign)
        {
            _context.BranchCampaigns.Add(branchCampaign);
            await _context.SaveChangesAsync();
            return branchCampaign;
        }

        public async Task<BranchCampaign?> GetByIdAsync(int id)
        {
            return await _context.BranchCampaigns
                .Include(bc => bc.Campaign)
                .Include(bc => bc.Branch)
                .FirstOrDefaultAsync(bc => bc.Id == id);
        }

        public async Task<BranchCampaign?> GetByBranchAndCampaignAsync(int branchId, int campaignId)
        {
            return await _context.BranchCampaigns
                .FirstOrDefaultAsync(bc => bc.BranchId == branchId && bc.CampaignId == campaignId);
        }

        public async Task UpdateAsync(BranchCampaign branchCampaign)
        {
            _context.BranchCampaigns.Update(branchCampaign);
            await _context.SaveChangesAsync();
        }
    }
}
