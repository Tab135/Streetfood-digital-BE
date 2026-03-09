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

        public async Task<Branch> GetByIdAsync(int branchId)
        {
            return await _context.Branches
                .AsNoTracking()
                .AsSplitQuery()
                .Include(b => b.Vendor)
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
                .Include(b => b.WorkSchedules)
                .Include(b => b.DayOffs)
                .Include(b => b.BranchImages)
                .ToListAsync();
        }

        public async Task<(List<Branch> items, int totalCount)> GetByVendorIdAsync(int vendorId, int pageNumber, int pageSize)
        {
            var query = _context.Branches
                .AsNoTracking()
                .Where(b => b.VendorId == vendorId);

            var totalCount = await query.CountAsync();
            
            var items = await query
                .AsSplitQuery()
                .Include(b => b.WorkSchedules)
                .Include(b => b.DayOffs)
                .Include(b => b.BranchImages)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<Branch> items, int totalCount)> GetAllAsync(int pageNumber, int pageSize)
        {
            var query = _context.Branches
                .AsNoTracking();

            var totalCount = await query.CountAsync();
            
            var items = await query
                .AsSplitQuery()
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
                .Include(b => b.Vendor)
                .Include(b => b.WorkSchedules)
                .Include(b => b.DayOffs)
                .Include(b => b.BranchImages)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
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

        public async Task<(List<BranchRegisterRequest> items, int totalCount)> GetAllBranchRegisterRequestsAsync(int pageNumber, int pageSize)
        {
            var query = _context.BranchRegisterRequests
                .AsNoTracking();

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
        /// Search vendors by keyword in branch name or dish name (case-insensitive).
        /// Returns branches grouped by vendor.
        /// </summary>
        public async Task<List<Branch>> SearchVendorsWithBranchesAndDishesAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new List<Branch>();
            }

            var searchPattern = $"%{keyword}%";

            var branches = await _context.Branches
                .AsNoTracking()
                .AsSplitQuery()
                .Where(b => b.IsActive && b.IsVerified &&
                    (EF.Functions.ILike(b.Name, searchPattern) ||
                     b.Dishes.Any(d => d.IsActive && EF.Functions.ILike(d.Name, searchPattern))))
                .Include(b => b.Vendor)
                .Include(b => b.Dishes.Where(d => d.IsActive))
                    .ThenInclude(d => d.Category)
                .OrderByDescending(b => b.AvgRating)
                .ThenBy(b => b.Name)
                .ToListAsync();

            return branches;
        }
    }
}
