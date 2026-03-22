using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class BranchDAO
    {
        private readonly StreetFoodDbContext _context;

        public BranchDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Branch> CreateAsync(Branch branch)
        {
            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();
            return branch;
        }

        public async Task<Branch?> GetByIdAsync(int branchId)
        {
            return await _context.Branches
                .AsNoTracking()
                .AsSplitQuery()
                .Include(b => b.Vendor)
                .Include(b => b.Tier)
                .Include(b => b.WorkSchedules)
                .Include(b => b.DayOffs)
                .Include(b => b.BranchImages)
                .FirstOrDefaultAsync(b => b.BranchId == branchId);
        }

        // Non-paginated version for internal use
        public async Task<List<Branch>> GetAllByVendorIdAsync(int vendorId)
        {
            return await _context.Branches
                .AsNoTracking()
                .AsSplitQuery()
                .Where(b => b.VendorId == vendorId)
                .Include(b => b.Tier)
                .Include(b => b.WorkSchedules)
                .Include(b => b.DayOffs)
                .Include(b => b.BranchImages)
                .ToListAsync();
        }

        public async Task<(List<Branch> items, int totalCount)> GetByCreatedByIdAsync(int userId, int pageNumber, int pageSize)
        {
            var query = _context.Branches
                .AsNoTracking()
                .Where(b => b.CreatedById == userId && b.VendorId == null);

            var totalCount = await query.CountAsync();

            var items = await query
                .AsSplitQuery()
                .Include(b => b.Tier)
                .Include(b => b.WorkSchedules)
                .Include(b => b.DayOffs)
                .Include(b => b.BranchImages)
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<Branch> items, int totalCount)> GetByVendorIdAsync(int vendorId, int pageNumber, int pageSize)
        {
            var query = _context.Branches
                .AsNoTracking()
                .Where(b => b.VendorId == vendorId);

            var totalCount = await query.CountAsync();

            var items = await query
                .AsSplitQuery()
                .Include(b => b.Tier)
                .Include(b => b.WorkSchedules)
                .Include(b => b.DayOffs)
                .Include(b => b.BranchImages)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<Branch>> GetAllByManagerIdAsync(int managerUserId)
        {
            return await _context.Branches
                .AsNoTracking()
                .AsSplitQuery()
                .Where(b => b.ManagerId == managerUserId)
                .Include(b => b.Tier)
                .Include(b => b.WorkSchedules)
                .Include(b => b.DayOffs)
                .Include(b => b.BranchImages)
                .ToListAsync();
        }

        public async Task<(List<Branch> items, int totalCount)> GetAllAsync(int pageNumber, int pageSize)
        {
            var query = _context.Branches
                .AsNoTracking();

            var totalCount = await query.CountAsync();

            var items = await query
                .AsSplitQuery()
                .Include(b => b.Tier)
                .Include(b => b.Vendor)
                .Include(b => b.WorkSchedules)
                .Include(b => b.DayOffs)
                .Include(b => b.BranchImages)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<Branch> items, int totalCount)> GetActiveBranchesAsync(int pageNumber, int pageSize)
        {
            var query = _context.Branches
                .AsNoTracking()
                .Where(b => b.IsActive && b.IsVerified); // Only return verified and active branches

            var totalCount = await query.CountAsync();

            var items = await query
                .AsSplitQuery()
                .Include(b => b.Tier)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<Branch>> GetByVerificationStatusAsync(bool isVerified)
        {
            return await _context.Branches
                .AsNoTracking()
                .Where(b => b.IsVerified == isVerified)
                .Include(b => b.Vendor)
                .ToListAsync();
        }

        public async Task<(List<Branch> items, int totalCount)> GetUnverifiedBranchesAsync(int pageNumber, int pageSize)
        {
            var query = _context.Branches
                .AsNoTracking()
                .Where(b => !b.IsVerified);

            var totalCount = await query.CountAsync();
            
            var items = await query
                .Include(b => b.Vendor)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task UpdateAsync(Branch branch)
        {
            branch.UpdatedAt = DateTime.UtcNow;
            if (branch.Vendor != null)
            {
                // if there is a vendor object, detach it so it can't conflict
                _context.Entry(branch.Vendor).State = EntityState.Detached;
                branch.Vendor = null;
            }

            _context.Branches.Update(branch);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int branchId)
        {
            var branch = await _context.Branches.FindAsync(branchId);
            if (branch != null)
            {
                _context.Branches.Remove(branch);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsByIdAsync(int branchId)
        {
            return await _context.Branches.AnyAsync(b => b.BranchId == branchId);
        }

        public async Task<List<WorkSchedule>> GetWorkSchedulesAsync(int branchId)
        {
            return await _context.WorkSchedules
                .AsNoTracking()
                .Where(ws => ws.BranchId == branchId)
                .ToListAsync();
        }

        public async Task<List<DayOff>> GetDayOffsAsync(int branchId)
        {
            return await _context.DayOffs
                .AsNoTracking()
                .Where(d => d.BranchId == branchId)
                .ToListAsync();
        }

        public async Task<(List<BranchImage> items, int totalCount)> GetBranchImagesAsync(int branchId, int pageNumber, int pageSize)
        {
            var query = _context.BranchImages
                .AsNoTracking()
                .Where(bi => bi.BranchId == branchId);

            var totalCount = await query.CountAsync();
            
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddWorkScheduleAsync(WorkSchedule workSchedule)
        {
            _context.WorkSchedules.Add(workSchedule);
            await _context.SaveChangesAsync();
        }

        public async Task<WorkSchedule> GetWorkScheduleByIdAsync(int scheduleId)
        {
            return await _context.WorkSchedules.FindAsync(scheduleId);
        }

        public async Task UpdateWorkScheduleAsync(WorkSchedule workSchedule)
        {
            _context.WorkSchedules.Update(workSchedule);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteWorkScheduleAsync(int scheduleId)
        {
            var schedule = await _context.WorkSchedules.FindAsync(scheduleId);
            if (schedule != null)
            {
                _context.WorkSchedules.Remove(schedule);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddDayOffAsync(DayOff dayOff)
        {
            _context.DayOffs.Add(dayOff);
            await _context.SaveChangesAsync();
        }

        public async Task<DayOff> GetDayOffByIdAsync(int dayOffId)
        {
            return await _context.DayOffs.FindAsync(dayOffId);
        }

        public async Task DeleteDayOffAsync(int dayOffId)
        {
            var dayOff = await _context.DayOffs.FindAsync(dayOffId);
            if (dayOff != null)
            {
                _context.DayOffs.Remove(dayOff);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddBranchImageAsync(BranchImage branchImage)
        {
            _context.BranchImages.Add(branchImage);
            await _context.SaveChangesAsync();
        }

        public async Task<BranchImage> GetBranchImageByIdAsync(int imageId)
        {
            return await _context.BranchImages.FindAsync(imageId);
        }

        public async Task DeleteBranchImageAsync(int imageId)
        {
            var image = await _context.BranchImages.FindAsync(imageId);
            if (image != null)
            {
                _context.BranchImages.Remove(image);
                await _context.SaveChangesAsync();
            }
        }

        // License registration methods
        public async Task<BranchRegisterRequest> GetBranchRegisterRequestAsync(int branchId)
        {
            return await _context.BranchRegisterRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.BranchId == branchId);
        }

        public async Task<Dictionary<int, BranchRegisterRequest>> GetRegisterRequestsByBranchIdsAsync(List<int> branchIds)
        {
            var requests = await _context.BranchRegisterRequests
                .AsNoTracking()
                .Where(r => branchIds.Contains(r.BranchId))
                .ToListAsync();
            return requests.ToDictionary(r => r.BranchId);
        }

        public async Task<(List<BranchRegisterRequest> items, int totalCount)> GetAllBranchRegisterRequestsAsync(int pageNumber, int pageSize)
        {
            var query = _context.BranchRegisterRequests
                .AsNoTracking()
                .Where(r => r.Status == RegisterVendorStatusEnum.Pending);

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(r => r.Branch)
                    .ThenInclude(b => b.BranchImages)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddBranchRegisterRequestAsync(BranchRegisterRequest request)
        {
            _context.BranchRegisterRequests.Add(request);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateBranchRegisterRequestAsync(BranchRegisterRequest request)
        {
            request.UpdatedAt = DateTime.UtcNow;
            _context.BranchRegisterRequests.Update(request);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Search vendors by keyword in vendor name or dish name (case-insensitive).
        /// Returns branches grouped by vendor.
        /// </summary>
        public async Task<List<Branch>> SearchVendorsWithBranchesAndDishesAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new List<Branch>();
            }

            var branches = await _context.Branches
                .AsNoTracking()
                .AsSplitQuery()
                .Where(b => b.IsActive && b.IsVerified)
                .Include(b => b.Vendor)
                .Include(b => b.Tier)
                .Include(b => b.BranchDishes.Where(bd => bd.Dish.IsActive))
                    .ThenInclude(bd => bd.Dish)
                        .ThenInclude(d => d.Category)
                .OrderByDescending(b => b.AvgRating)
                .ThenBy(b => b.Name)
                .ToListAsync();

            return branches;
        }

        /// <summary>
        /// Get all active branches without any filtering (used when no filters provided).
        /// Returns all active and verified branches with their dishes.
        /// </summary>
        public async Task<List<Branch>> GetAllActiveBranchesWithoutFilterAsync()
        {
            return await _context.Branches
                .AsNoTracking()
                .AsSplitQuery()
                .Where(b => b.IsActive && b.IsVerified)                  .Include(b => b.Tier)                .Include(b => b.Vendor)
                    .ThenInclude(v => v.VendorDietaryPreferences)
                        .ThenInclude(vdp => vdp.DietaryPreference)
                .Include(b => b.BranchDishes.Where(bd => bd.Dish.IsActive))
                    .ThenInclude(bd => bd.Dish)
                        .ThenInclude(d => d.Category)
                .Include(b => b.BranchDishes.Where(bd => bd.Dish.IsActive))
                    .ThenInclude(bd => bd.Dish)
                        .ThenInclude(d => d.DishTastes)
                            .ThenInclude(dt => dt.Taste)
                .OrderByDescending(b => b.AvgRating)
                .ThenBy(b => b.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Get active branches with dynamic filtering: distance (Haversine), dietary, taste, price range.
        /// </summary>
        public async Task<List<(Branch branch, double distanceKm)>> GetActiveBranchesFilteredAsync(
            double? userLat,
            double? userLong,
            double? maxDistanceKm,
            List<int>? dietaryIds,
            List<int>? tasteIds,
            decimal? minPrice,
            decimal? maxPrice,
            List<int>? categoryIds)
        {
            var branches = await _context.Branches
                .AsNoTracking()
                .AsSplitQuery()
                .Where(b => b.IsActive && b.IsVerified)
                .Include(b => b.Vendor)
                    .ThenInclude(v => v.VendorDietaryPreferences)
                        .ThenInclude(vdp => vdp.DietaryPreference)
                .Include(b => b.BranchDishes.Where(bd => bd.Dish.IsActive))
                    .ThenInclude(bd => bd.Dish)
                        .ThenInclude(d => d.Category)
                .Include(b => b.BranchDishes.Where(bd => bd.Dish.IsActive))
                    .ThenInclude(bd => bd.Dish)
                        .ThenInclude(d => d.DishTastes)
                            .ThenInclude(dt => dt.Taste)
                .ToListAsync();

            bool hasDietaryFilter  = dietaryIds  != null && dietaryIds.Count  > 0;
            bool hasTasteFilter    = tasteIds    != null && tasteIds.Count    > 0;
            bool hasPriceFilter    = minPrice.HasValue || maxPrice.HasValue;
            bool hasCategoryFilter = categoryIds != null && categoryIds.Count > 0;
            bool hasAnyFilter = hasDietaryFilter || hasTasteFilter || hasPriceFilter || hasCategoryFilter;

            var filteredBranches = new List<(Branch branch, double distanceKm)>();

            foreach (var branch in branches)
            {
                double distanceKm = 0;
                bool hasGps = userLat.HasValue && userLong.HasValue;

                if (hasGps)
                {
                    distanceKm = HaversineDistance(userLat.Value, userLong.Value, branch.Lat, branch.Long);

                    // Distance filter
                    double maxDist = maxDistanceKm ?? 10.0;
                    if (distanceKm > maxDist)
                        continue;
                }

                // If no filters provided, include all branches within distance
                if (!hasAnyFilter)
                {
                    filteredBranches.Add((branch, distanceKm));
                    continue;
                }

                // Dietary filter: checked at vendor level (not dish level)
                if (hasDietaryFilter)
                {
                    var vendorDietaryIds = branch.Vendor.VendorDietaryPreferences
                        .Select(vdp => vdp.DietaryPreferenceId).ToHashSet();
                    if (!dietaryIds!.Any(vendorDietaryIds.Contains))
                        continue;
                }

                // Dish-level filters: taste, price, category
                bool hasDishLevelFilter = hasTasteFilter || hasPriceFilter || hasCategoryFilter;
                if (hasDishLevelFilter)
                {
                    bool hasQualifyingDish = branch.BranchDishes
                        .Select(bd => bd.Dish)
                        .Any(dish =>
                        {
                            if (minPrice.HasValue && dish.Price < minPrice.Value) return false;
                            if (maxPrice.HasValue && dish.Price > maxPrice.Value) return false;
                            if (hasCategoryFilter && !categoryIds!.Contains(dish.CategoryId)) return false;
                            if (!hasTasteFilter) return true;

                            var dishTasteIds = dish.DishTastes.Select(dt => dt.TasteId).ToHashSet();
                            return tasteIds!.Any(id => dishTasteIds.Contains(id));
                        });

                    if (!hasQualifyingDish)
                        continue;
                }

                filteredBranches.Add((branch, distanceKm));
            }

            // Sort by distance (nearest first) and return all
            return filteredBranches
                .OrderBy(x => x.distanceKm)
                .ToList();
        }

        public async Task UpdateBranchMetricsAndTierAsync(int branchId, int rating, int newBatchReviewCount, int newBatchRatingSum, int newTierId, bool banBranch)
        {
            await _context.Branches
                .Where(b => b.BranchId == branchId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.TotalReviewCount, b => b.TotalReviewCount + 1)
                    .SetProperty(b => b.TotalRatingSum, b => b.TotalRatingSum + rating)
                    .SetProperty(b => b.AvgRating, b => (double)(b.TotalRatingSum + rating) / (b.TotalReviewCount + 1))
                    .SetProperty(b => b.BatchReviewCount, b => newBatchReviewCount)
                    .SetProperty(b => b.BatchRatingSum, b => newBatchRatingSum)
                    .SetProperty(b => b.TierId, b => newTierId)
                    .SetProperty(b => b.IsActive, b => banBranch ? false : b.IsActive)
                );
        }

        public async Task UpdateBranchMetricsOnFeedbackUpdatedAsync(int branchId, int oldRating, int newRating)
        {
            if (oldRating == newRating) return;
            int delta = newRating - oldRating;

            await _context.Branches
                .Where(b => b.BranchId == branchId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.TotalRatingSum, b => b.TotalRatingSum + delta)
                    .SetProperty(b => b.AvgRating, b => b.TotalReviewCount > 0 
                        ? (double)(b.TotalRatingSum + delta) / b.TotalReviewCount 
                        : 0)
                );
        }

        public async Task UpdateBranchMetricsOnFeedbackDeletedAsync(int branchId, int rating)
        {
            await _context.Branches
                .Where(b => b.BranchId == branchId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.TotalReviewCount, b => b.TotalReviewCount - 1 < 0 ? 0 : b.TotalReviewCount - 1)
                    .SetProperty(b => b.TotalRatingSum, b => b.TotalRatingSum - rating < 0 ? 0 : b.TotalRatingSum - rating)
                    .SetProperty(b => b.AvgRating, b => b.TotalReviewCount - 1 > 0 
                        ? (double)(b.TotalRatingSum - rating) / (b.TotalReviewCount - 1) 
                        : 0)
                );
        }

        public async Task RecalculateBranchMetricsAsync(int branchId)
        {
            var metrics = await _context.Feedbacks
                .Where(f => f.BranchId == branchId)
                .GroupBy(f => f.BranchId)
                .Select(g => new { Count = g.Count(), Total = g.Sum(x => (int?)x.Rating) ?? 0 })
                .FirstOrDefaultAsync();

            int newCount = metrics?.Count ?? 0;
            int newTotal = metrics?.Total ?? 0;
            double newAvg = newCount > 0 ? (double)newTotal / newCount : 0;

            var latest20Feedbacks = await _context.Feedbacks
                .Where(f => f.BranchId == branchId)
                .OrderByDescending(f => f.CreatedAt)
                .Take(20)
                .Select(f => f.Rating)
                .ToListAsync();

            int batchCount = latest20Feedbacks.Count;
            int batchTotal = latest20Feedbacks.Sum();

            await _context.Branches
                .Where(b => b.BranchId == branchId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.TotalReviewCount, newCount)
                    .SetProperty(b => b.TotalRatingSum, newTotal)
                    .SetProperty(b => b.AvgRating, newAvg)
                    .SetProperty(b => b.BatchReviewCount, batchCount)
                    .SetProperty(b => b.BatchRatingSum, batchTotal)
                );
        }

        /// <summary>
        /// Calculate the great-circle distance between two points using the Haversine formula.
        /// Returns distance in kilometers.
        /// </summary>
        private static double HaversineDistance(double lat1, double long1, double lat2, double long2)
        {
            const double EarthRadiusKm = 6371.0;

            double dLat = DegreesToRadians(lat2 - lat1);
            double dLong = DegreesToRadians(long2 - long1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                       Math.Sin(dLong / 2) * Math.Sin(dLong / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }

        public async Task<(List<Branch> items, int totalCount)> GetAllApprovedGhostPinsAsync(int pageNumber, int pageSize)
        {
            var query = _context.Branches
                .AsNoTracking()
                .Where(b => b.VendorId == null && b.IsVerified);

            var totalCount = await query.CountAsync();

            var items = await query
                .AsSplitQuery()
                .Include(b => b.Tier)
                .Include(b => b.Vendor)
                .Include(b => b.Manager)
                .Include(b => b.CreatedBy)
                .Include(b => b.WorkSchedules)
                .Include(b => b.DayOffs)
                .Include(b => b.BranchImages)
                .Include(b => b.BranchDishes)
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
