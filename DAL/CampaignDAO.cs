using BO.DTO.Campaigns;
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
                .Where(c => c.CreatedByVendorId == null && c.EndDate >= DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<List<Campaign>> GetByBranchIdAsync(int branchId)
        {
            return await _context.Campaigns
                .Where(c => c.BranchCampaigns.Any(bc => bc.BranchId == branchId))
                .ToListAsync();
        }

        public async Task UpdateAsync(Campaign campaign)
        {
            _context.Campaigns.Update(campaign);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int campaignId)
        {
            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign == null)
            {
                return;
            }

            _context.Campaigns.Remove(campaign);
            await _context.SaveChangesAsync();
        }

        public async Task<List<int>> GetCampaignIdsToActivateAsync(DateTime now)
        {
            return await _context.Campaigns
                .Where(c => !c.IsActive
                            && c.StartDate <= now
                            && c.EndDate >= now
                            && _context.BranchCampaigns.Any(bc => bc.CampaignId == c.CampaignId))
                .Select(c => c.CampaignId)
                .ToListAsync();
        }

        public async Task<List<int>> GetExpiredCampaignIdsAsync(DateTime now)
        {
            return await _context.Campaigns
                .Where(c => c.IsActive && c.EndDate < now)
                .Select(c => c.CampaignId)
                .ToListAsync();
        }

        public async Task<List<int>> GetCampaignIdsToOpenRegistrationAsync(DateTime now)
        {
            return await _context.Campaigns
                .Where(c => c.CreatedByVendorId == null
                            && !c.IsRegisterable
                            && c.RegistrationStartDate != null
                            && c.RegistrationStartDate <= now
                            && (c.RegistrationEndDate == null || c.RegistrationEndDate >= now))
                .Select(c => c.CampaignId)
                .ToListAsync();
        }

        public async Task<List<int>> GetCampaignIdsToCloseRegistrationAsync(DateTime now)
        {
            return await _context.Campaigns
                .Where(c => c.CreatedByVendorId == null
                            && c.IsRegisterable
                            && (c.RegistrationStartDate == null
                                || c.RegistrationStartDate > now
                                || (c.RegistrationEndDate != null && c.RegistrationEndDate < now)))
                .Select(c => c.CampaignId)
                .ToListAsync();
        }

        public async Task<(List<Campaign> Items, int TotalCount)> GetCampaignsAsync(bool? isSystem, int? vendorId, int page, int pageSize)
        {
            var query = _context.Campaigns.AsQueryable();

            if (isSystem == true)
            {
                query = query.Where(c => c.CreatedByVendorId == null);
            }
            if (vendorId.HasValue)
            {
                // Fetch campaigns created by the vendor OR created by any branch owned by this vendor
                query = query.Where(c => c.CreatedByVendorId == vendorId.Value || 
                                        // Additionally include system campaigns that the vendor has joined and paid
                                        (c.CreatedByVendorId == null &&
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
            var query = _context.Campaigns
                .Where(c => c.CreatedByVendorId == null) // system campaigns
                .Where(c => c.IsRegisterable);

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
            var query = _context.Campaigns
                .Where(c => c.IsActive && c.StartDate <= now && c.EndDate >= now);

            if (isSystem.HasValue)
            {
                query = isSystem.Value
                    ? query.Where(c => c.CreatedByVendorId == null)
                    : query.Where(c => c.CreatedByVendorId != null);
            }

            int totalCount = await query.CountAsync();
            var items = await query.OrderByDescending(c => c.CreatedAt)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<CampaignBranchResponseDto> Items, int TotalCount)> GetBranchesInAnyVendorCampaignPaginatedAsync(int pageNumber, int pageSize, double? userLat, double? userLng, double? maxDistance = 5.0)
        {
            var branches = await _context.Branches
                .AsNoTracking()
                .Include(b => b.Tier)
                .Include(b => b.Vendor)
                .Where(b => _context.BranchCampaigns
                    .Any(bc => bc.BranchId == b.BranchId && bc.IsActive 
                        && bc.Campaign.CreatedByVendorId != null 
                        && bc.Campaign.IsActive 
                        && _context.Vouchers.Any(v => v.VendorCampaignId == bc.CampaignId)))
                .ToListAsync();

            var branchIds = branches.Select(b => b.BranchId).ToList();

            var branchCampaigns = await _context.BranchCampaigns
                .AsNoTracking()
                .Where(bc => branchIds.Contains(bc.BranchId)
                    && bc.IsActive
                    && bc.Campaign.CreatedByVendorId != null
                    && bc.Campaign.IsActive
                    && _context.Vouchers.Any(v => v.VendorCampaignId == bc.CampaignId))
                .Include(bc => bc.Campaign)
                .ToListAsync();

            var campaignIds = branchCampaigns.Select(bc => bc.CampaignId).Distinct().ToList();

            var vouchers = campaignIds.Count > 0
                ? await _context.Vouchers
                    .AsNoTracking()
                    .Where(v => v.VendorCampaignId.HasValue && campaignIds.Contains(v.VendorCampaignId.Value))
                    .ToListAsync()
                : new List<Voucher>();

            return MapAndPaginateBranchesWithCampaigns(branches, branchCampaigns, vouchers, pageNumber, pageSize, userLat, userLng, maxDistance);
        }

        public async Task<(List<CampaignBranchResponseDto> Items, int TotalCount)> GetCampaignBranchesPaginatedAsync(int campaignId, int pageNumber, int pageSize, double? userLat, double? userLng, bool includeInactiveBranches = false)
        {
            var branches = await _context.Branches
                .AsNoTracking()
                .Include(b => b.Tier)
                .Include(b => b.Vendor)
                .Where(b => _context.BranchCampaigns
                    .Any(bc => bc.CampaignId == campaignId && bc.BranchId == b.BranchId && (includeInactiveBranches || bc.IsActive)))
                .ToListAsync();

            return MapAndPaginateBranches(branches, pageNumber, pageSize, userLat, userLng, null);
        }

        private (List<CampaignBranchResponseDto> Items, int TotalCount) MapAndPaginateBranches(List<Branch> branches, int pageNumber, int pageSize, double? userLat, double? userLng, double? maxDistance = null)
        {
            var branchList = branches.Select(b => 
            {
                double distanceKm = 0;
                if (userLat.HasValue && userLng.HasValue)
                {
                    distanceKm = Math.Round(HaversineDistance(userLat.Value, userLng.Value, b.Lat, b.Long), 2);
                }

                double wDist = 0.6;
                double wRate = 0.4;
                double tierWeight = b.Tier != null ? b.Tier.Weight : 1.0;
                double subMultiplier = b.IsSubscribed ? 1.2 : 0.7;

                double distanceScore = (distanceKm == 0 && (!userLat.HasValue || !userLng.HasValue))
                    ? 0
                    : (1 / (distanceKm + 1)) * wDist;

                double ratingScore = (b.AvgRating / 5) * wRate;
                double finalScore = Math.Round((distanceScore + ratingScore) * tierWeight * subMultiplier, 4);

                return new CampaignBranchResponseDto
                {
                    BranchId = b.BranchId,
                    VendorId = b.VendorId ?? 0,
                    VendorName = b.Vendor?.Name ?? string.Empty,
                    ManagerId = b.ManagerId,
                    Name = b.Name,
                    PhoneNumber = b.PhoneNumber,
                    Email = b.Email,
                    AddressDetail = b.AddressDetail,
                    Ward = b.Ward,
                    City = b.City,
                    Lat = b.Lat,
                    Long = b.Long,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt,
                    IsVerified = b.IsVerified,
                    AvgRating = b.AvgRating,
                    TotalReviewCount = b.TotalReviewCount,
                    IsActive = b.IsActive,
                    TierId = b.TierId,
                    TierName = b.Tier?.Name,
                    CreatedById = b.CreatedById,
                    TotalRatingSum = b.TotalRatingSum,
                    IsSubscribed = b.IsSubscribed,
                    SubscriptionExpiresAt = b.SubscriptionExpiresAt,
                    LastTierResetAt = b.LastTierResetAt,
                    GhostpinXP = b.GhostpinXP,
                    BatchReviewCount = b.BatchReviewCount,
                    BatchRatingSum = b.BatchRatingSum,
                    FinalScore = finalScore,
                    DistanceKm = (userLat.HasValue && userLng.HasValue) ? distanceKm : null
                };
            })
            .Where(x => !maxDistance.HasValue || !x.DistanceKm.HasValue || x.DistanceKm.Value <= maxDistance.Value)
            .OrderByDescending(x => x.FinalScore)
            .ToList();

            var totalCount = branchList.Count;
            var items = branchList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (items, totalCount);
        }

        private (List<CampaignBranchResponseDto> Items, int TotalCount) MapAndPaginateBranchesWithCampaigns(
            List<Branch> branches,
            List<BranchCampaign> branchCampaigns,
            List<Voucher> vouchers,
            int pageNumber, int pageSize, double? userLat, double? userLng, double? maxDistance = null)
        {
            var now = DateTime.UtcNow;

            var vouchersByCampaign = vouchers
                .GroupBy(v => v.VendorCampaignId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var campaignsByBranch = branchCampaigns
                .GroupBy(bc => bc.BranchId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var branchList = branches.Select(b =>
            {
                double distanceKm = 0;
                if (userLat.HasValue && userLng.HasValue)
                    distanceKm = Math.Round(HaversineDistance(userLat.Value, userLng.Value, b.Lat, b.Long), 2);

                double tierWeight = b.Tier != null ? b.Tier.Weight : 1.0;
                double subMultiplier = b.IsSubscribed ? 1.2 : 0.7;
                double distanceScore = (distanceKm == 0 && (!userLat.HasValue || !userLng.HasValue))
                    ? 0
                    : (1 / (distanceKm + 1)) * 0.6;
                double ratingScore = (b.AvgRating / 5) * 0.4;
                double finalScore = Math.Round((distanceScore + ratingScore) * tierWeight * subMultiplier, 4);

                var campaigns = campaignsByBranch.TryGetValue(b.BranchId, out var bcs)
                    ? bcs.Where(bc => vouchersByCampaign.ContainsKey(bc.CampaignId))
                        .Select(bc => new BranchCampaignInfoDto
                    {
                        CampaignId = bc.CampaignId,
                        Name = bc.Campaign.Name,
                        Description = bc.Campaign.Description,
                        ImageUrl = bc.Campaign.ImageUrl,
                        StartDate = bc.Campaign.StartDate,
                        EndDate = bc.Campaign.EndDate,
                        IsActive = bc.Campaign.IsActive,
                        IsRegisterable = bc.Campaign.CreatedByVendorId == null
                            && bc.Campaign.RegistrationStartDate.HasValue
                            && now >= bc.Campaign.RegistrationStartDate.Value
                            && (!bc.Campaign.RegistrationEndDate.HasValue || now <= bc.Campaign.RegistrationEndDate.Value),
                        IsWorking = bc.Campaign.IsActive
                            && bc.Campaign.StartDate <= now
                            && bc.Campaign.EndDate >= now,
                        Vouchers = vouchersByCampaign.TryGetValue(bc.CampaignId, out var vs)
                            ? vs.Select(v => new CampaignVoucherInfoDto
                            {
                                VoucherId = v.VoucherId,
                                Name = v.Name,
                                Type = v.Type,
                                DiscountValue = v.DiscountValue,
                                MinAmountRequired = v.MinAmountRequired,
                                MaxDiscountValue = v.MaxDiscountValue,
                                Quantity = v.Quantity,
                                UsedQuantity = v.UsedQuantity,
                                StartDate = v.StartDate,
                                EndDate = v.EndDate,
                                VoucherCode = v.VoucherCode
                            }).ToList()
                            : []
                    }).ToList()
                    : [];

                return new CampaignBranchResponseDto
                {
                    BranchId = b.BranchId,
                    VendorId = b.VendorId ?? 0,
                    VendorName = b.Vendor?.Name ?? string.Empty,
                    ManagerId = b.ManagerId,
                    Name = b.Name,
                    PhoneNumber = b.PhoneNumber,
                    Email = b.Email,
                    AddressDetail = b.AddressDetail,
                    Ward = b.Ward,
                    City = b.City,
                    Lat = b.Lat,
                    Long = b.Long,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt,
                    IsVerified = b.IsVerified,
                    AvgRating = b.AvgRating,
                    TotalReviewCount = b.TotalReviewCount,
                    IsActive = b.IsActive,
                    TierId = b.TierId,
                    TierName = b.Tier?.Name,
                    CreatedById = b.CreatedById,
                    TotalRatingSum = b.TotalRatingSum,
                    IsSubscribed = b.IsSubscribed,
                    SubscriptionExpiresAt = b.SubscriptionExpiresAt,
                    LastTierResetAt = b.LastTierResetAt,
                    GhostpinXP = b.GhostpinXP,
                    BatchReviewCount = b.BatchReviewCount,
                    BatchRatingSum = b.BatchRatingSum,
                    FinalScore = finalScore,
                    DistanceKm = (userLat.HasValue && userLng.HasValue) ? distanceKm : null,
                    Campaigns = campaigns
                };
            })
            .Where(x => !maxDistance.HasValue || !x.DistanceKm.HasValue || x.DistanceKm.Value <= maxDistance.Value)
            .OrderByDescending(x => x.FinalScore)
            .ToList();

            var totalCount = branchList.Count;
            var items = branchList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (items, totalCount);
        }

        private static double HaversineDistance(double lat1, double long1, double lat2, double long2)
        {
            const double R = 6371; // Earth radius in km
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLong = (long2 - long1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLong / 2) * Math.Sin(dLong / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        // --- Campaign Image Methods ---
    }
}
