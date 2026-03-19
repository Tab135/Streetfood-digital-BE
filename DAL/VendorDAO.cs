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
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();
            return vendor;
        }

        public async Task<Vendor> GetByIdAsync(int vendorId)
        {
            return await _context.Vendors
                .Include(v => v.VendorOwner)
                .Include(v => v.VendorDietaryPreferences)
                    .ThenInclude(vdp => vdp.DietaryPreference)
                .FirstOrDefaultAsync(v => v.VendorId == vendorId);
        }

        public async Task<Vendor> GetByUserIdAsync(int userId)
        {
            return await _context.Vendors
                .Include(v => v.VendorOwner)
                .Include(v => v.VendorDietaryPreferences)
                    .ThenInclude(vdp => vdp.DietaryPreference)
                .FirstOrDefaultAsync(v => v.UserId == userId);
        }

        public async Task<(List<Vendor> items, int totalCount)> GetAllAsync(int pageNumber, int pageSize)
        {
            var query = _context.Vendors;

            var totalCount = await query.CountAsync();
            
            var items = await query
                .Include(v => v.VendorOwner)
                .Include(v => v.VendorDietaryPreferences)
                    .ThenInclude(vdp => vdp.DietaryPreference)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<Vendor> items, int totalCount)> GetActiveVendorsAsync(int pageNumber, int pageSize)
        {
            var query = _context.Vendors
                .Where(v => v.IsActive);

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(v => v.VendorOwner)
                .Include(v => v.VendorDietaryPreferences)
                    .ThenInclude(vdp => vdp.DietaryPreference)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
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
    }
}
