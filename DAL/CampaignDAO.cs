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

                public async Task<(List<Campaign> Items, int TotalCount)> GetCampaignsAsync(bool? isSystem, int? vendorId, int page, int pageSize)
        {
            var query = _context.Campaigns.Include(c => c.CreatedByBranch).AsQueryable();

            if (isSystem == true)
            {
                query = query.Where(c => c.CreatedByBranchId == null && c.CreatedByVendorId == null);
            }
            if (vendorId.HasValue)
            {
                // Fetch campaigns created by the vendor OR created by any branch owned by this vendor
                query = query.Where(c => c.CreatedByVendorId == vendorId.Value || 
                                        (c.CreatedByBranchId != null && c.CreatedByBranch.VendorId == vendorId.Value) ||
                                        // Additionally include system campaigns that the vendor has joined and paid
                                        (c.CreatedByBranchId == null && c.CreatedByVendorId == null &&
                                         c.BranchCampaigns.Any(bc => bc.Branch.VendorId == vendorId.Value && bc.IsActive)));
            }

            int totalCount = await query.CountAsync();
            var items = await query.OrderByDescending(c => c.CreatedAt)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<Campaign> Items, int TotalCount)> GetJoinableSystemCampaignsAsync(int page, int pageSize)
        {
            var now = DateTime.UtcNow;
            var query = _context.Campaigns
                .Where(c => c.CreatedByBranchId == null && c.CreatedByVendorId == null) // system campaigns
                .Where(c => c.IsActive)
                .Where(c => c.RegistrationStartDate != null && c.RegistrationEndDate != null 
                            && now >= c.RegistrationStartDate && now <= c.RegistrationEndDate);

            int totalCount = await query.CountAsync();
            var items = await query.OrderByDescending(c => c.CreatedAt)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<Campaign> Items, int TotalCount)> GetPublicCampaignsAsync(bool? isSystem, int page, int pageSize)
        {
            var now = DateTime.UtcNow;
            var query = _context.Campaigns.Include(c => c.CreatedByBranch)
                .Where(c => c.IsActive && c.StartDate <= now && c.EndDate >= now);

            if (isSystem.HasValue)
            {
                query = isSystem.Value
                    ? query.Where(c => c.CreatedByBranchId == null && c.CreatedByVendorId == null)
                    : query.Where(c => c.CreatedByBranchId != null || c.CreatedByVendorId != null);
            }

            int totalCount = await query.CountAsync();
            var items = await query.OrderByDescending(c => c.CreatedAt)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return (items, totalCount);
        }

        // --- Campaign Image Methods ---
    }
}
