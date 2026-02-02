using BO.Entities;
using DAL;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class VendorRepository : IVendorRepository
    {
        private readonly VendorDAO _vendorDAO;

        public VendorRepository(VendorDAO vendorDAO)
        {
            _vendorDAO = vendorDAO ?? throw new ArgumentNullException(nameof(vendorDAO));
        }

        public async Task<Vendor> CreateAsync(Vendor vendor)
        {
            return await _vendorDAO.CreateAsync(vendor);
        }

        public async Task<Vendor> GetByIdAsync(int vendorId)
        {
            return await _vendorDAO.GetByIdAsync(vendorId);
        }

        public async Task<Vendor> GetByUserIdAsync(int userId)
        {
            return await _vendorDAO.GetByUserIdAsync(userId);
        }

        public async Task<List<Vendor>> GetAllAsync()
        {
            return await _vendorDAO.GetAllAsync();
        }

        public async Task<List<Vendor>> GetActiveVendorsAsync()
        {
            return await _vendorDAO.GetActiveVendorsAsync();
        }

        public async Task<List<Vendor>> GetByVerificationStatusAsync(bool isVerified)
        {
            return await _vendorDAO.GetByVerificationStatusAsync(isVerified);
        }

        public async Task UpdateAsync(Vendor vendor)
        {
            await _vendorDAO.UpdateAsync(vendor);
        }

        public async Task DeleteAsync(int vendorId)
        {
            await _vendorDAO.DeleteAsync(vendorId);
        }

        public async Task<bool> ExistsByIdAsync(int vendorId)
        {
            return await _vendorDAO.ExistsByIdAsync(vendorId);
        }

        public async Task<bool> ExistsByUserIdAsync(int userId)
        {
            return await _vendorDAO.ExistsByUserIdAsync(userId);
        }

        public async Task<List<WorkSchedule>> GetWorkSchedulesAsync(int vendorId)
        {
            return await _vendorDAO.GetWorkSchedulesAsync(vendorId);
        }

        public async Task<List<DayOff>> GetDayOffsAsync(int vendorId)
        {
            return await _vendorDAO.GetDayOffsAsync(vendorId);
        }

        public async Task<List<VendorImage>> GetVendorImagesAsync(int vendorId)
        {
            return await _vendorDAO.GetVendorImagesAsync(vendorId);
        }

        public async Task AddWorkScheduleAsync(WorkSchedule workSchedule)
        {
            await _vendorDAO.AddWorkScheduleAsync(workSchedule);
        }

        public async Task AddDayOffAsync(DayOff dayOff)
        {
            await _vendorDAO.AddDayOffAsync(dayOff);
        }

        public async Task AddVendorImageAsync(VendorImage vendorImage)
        {
            await _vendorDAO.AddVendorImageAsync(vendorImage);
        }

        public async Task<VendorRegisterRequest> GetVendorRegisterRequestAsync(int vendorId)
        {
            return await _vendorDAO.GetVendorRegisterRequestAsync(vendorId);
        }

        public async Task<List<VendorRegisterRequest>> GetAllVendorRegisterRequestsAsync()
        {
            return await _vendorDAO.GetAllVendorRegisterRequestsAsync();
        }

        public async Task AddVendorRegisterRequestAsync(VendorRegisterRequest request)
        {
            await _vendorDAO.AddVendorRegisterRequestAsync(request);
        }

        public async Task UpdateVendorRegisterRequestAsync(VendorRegisterRequest request)
        {
            await _vendorDAO.UpdateVendorRegisterRequestAsync(request);
        }
    }
}
