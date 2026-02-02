using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class VendorDAO
    {
        private readonly StreetFoodDbContext _context;

        public VendorDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Vendor> CreateAsync(Vendor vendor)
        {
            vendor.CreatedAt = DateTime.UtcNow;
            vendor.IsVerified = false; // Business rule: default to not verified
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();
            return vendor;
        }

        public async Task<Vendor> GetByIdAsync(int vendorId)
        {
            return await _context.Vendors
                .Include(v => v.VendorOwner)
                .FirstOrDefaultAsync(v => v.VendorId == vendorId);
        }

        public async Task<Vendor> GetByUserIdAsync(int userId)
        {
            return await _context.Vendors
                .Include(v => v.VendorOwner)
                .FirstOrDefaultAsync(v => v.UserId == userId);
        }

        public async Task<List<Vendor>> GetAllAsync()
        {
            return await _context.Vendors
                .Include(v => v.VendorOwner)
                .ToListAsync();
        }

        public async Task<List<Vendor>> GetActiveVendorsAsync()
        {
            return await _context.Vendors
                .Include(v => v.VendorOwner)
                .Where(v => v.IsActive && v.IsVerified)
                .ToListAsync();
        }

        public async Task<List<Vendor>> GetByVerificationStatusAsync(bool isVerified)
        {
            return await _context.Vendors
                .Include(v => v.VendorOwner)
                .Where(v => v.IsVerified == isVerified)
                .ToListAsync();
        }

        public async Task UpdateAsync(Vendor vendor)
        {
            vendor.UpdatedAt = DateTime.UtcNow;
            _context.Vendors.Update(vendor);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int vendorId)
        {
            var vendor = await GetByIdAsync(vendorId);
            if (vendor != null)
            {
                _context.Vendors.Remove(vendor);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsByIdAsync(int vendorId)
        {
            return await _context.Vendors
                .AnyAsync(v => v.VendorId == vendorId);
        }

        public async Task<bool> ExistsByUserIdAsync(int userId)
        {
            return await _context.Vendors
                .AnyAsync(v => v.UserId == userId);
        }

        // Related entities queries
        public async Task<List<WorkSchedule>> GetWorkSchedulesAsync(int vendorId)
        {
            return await _context.WorkSchedules
                .Where(ws => ws.VendorId == vendorId)
                .ToListAsync();
        }

        public async Task<List<DayOff>> GetDayOffsAsync(int vendorId)
        {
            return await _context.DayOffs
                .Where(d => d.VendorId == vendorId)
                .ToListAsync();
        }

        public async Task<List<VendorImage>> GetVendorImagesAsync(int vendorId)
        {
            return await _context.VendorImages
                .Where(vi => vi.VendorId == vendorId)
                .ToListAsync();
        }

        public async Task AddWorkScheduleAsync(WorkSchedule workSchedule)
        {
            _context.WorkSchedules.Add(workSchedule);
            await _context.SaveChangesAsync();
        }

        public async Task AddDayOffAsync(DayOff dayOff)
        {
            _context.DayOffs.Add(dayOff);
            await _context.SaveChangesAsync();
        }

        public async Task AddVendorImageAsync(VendorImage vendorImage)
        {
            _context.VendorImages.Add(vendorImage);
            await _context.SaveChangesAsync();
        }

        public async Task<VendorRegisterRequest> GetVendorRegisterRequestAsync(int vendorId)
        {
            return await _context.VendorRegisterRequests
                .FirstOrDefaultAsync(vrr => vrr.VendorId == vendorId);
        }

        public async Task<List<VendorRegisterRequest>> GetAllVendorRegisterRequestsAsync()
        {
            return await _context.VendorRegisterRequests
                .ToListAsync();
        }

        public async Task AddVendorRegisterRequestAsync(VendorRegisterRequest request)
        {
            _context.VendorRegisterRequests.Add(request);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateVendorRegisterRequestAsync(VendorRegisterRequest request)
        {
            _context.VendorRegisterRequests.Update(request);
            await _context.SaveChangesAsync();
        }
    }
}
