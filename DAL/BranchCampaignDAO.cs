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

        public async Task<List<BranchCampaign>> GetPendingByCampaignAndVendorAsync(int campaignId, int vendorId)
        {
            // Only return pending (not yet paid) rows for this vendor + campaign
            return await _context.BranchCampaigns
                .Include(bc => bc.Campaign)
                .Include(bc => bc.Branch)
                .Where(bc =>
                    bc.CampaignId == campaignId &&
                    bc.IsActive == false &&
                    bc.Branch.VendorId == vendorId)
                .ToListAsync();
        }

        public async Task UpdateAsync(BranchCampaign branchCampaign)
        {
            _context.BranchCampaigns.Update(branchCampaign);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteByBranchAndCampaignAsync(int branchId, int campaignId)
        {
            var bc = await GetByBranchAndCampaignAsync(branchId, campaignId);
            if (bc == null)
                return false;
            _context.BranchCampaigns.Remove(bc);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>Branch IDs participating in this campaign for the given vendor (BranchCampaign rows).</summary>
        public async Task<List<int>> GetBranchIdsByCampaignAndVendorAsync(int campaignId, int vendorId)
        {
            return await _context.BranchCampaigns
                .AsNoTracking()
                .Where(bc => bc.CampaignId == campaignId && bc.Branch.VendorId == vendorId)
                .OrderBy(bc => bc.BranchId)
                .Select(bc => bc.BranchId)
                .ToListAsync();
        }

        public Task<int> CountByCampaignIdAsync(int campaignId) =>
            _context.BranchCampaigns.AsNoTracking().CountAsync(bc => bc.CampaignId == campaignId);

        public async Task SetAllIsActiveForCampaignAsync(int campaignId, bool isActive)
        {
            await _context.BranchCampaigns
                .Where(bc => bc.CampaignId == campaignId)
                .ExecuteUpdateAsync(s => s.SetProperty(bc => bc.IsActive, isActive));
        }

        public async Task<List<BranchCampaign>> GetActiveByBranchIdsWithCampaignAsync(List<int> branchIds)
        {
            return await _context.BranchCampaigns
                .AsNoTracking()
                .Where(bc => branchIds.Contains(bc.BranchId)
                    && bc.IsActive
                    && bc.Campaign.CreatedByVendorId != null
                    && bc.Campaign.IsActive)
                .Include(bc => bc.Campaign)
                .ToListAsync();
        }
    }
}
