using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IVendorRepository
    {
        Task<Vendor> CreateAsync(Vendor vendor);
        Task<Vendor> GetByIdAsync(int vendorId);
        Task<Vendor> GetByUserIdAsync(int userId);
        Task<List<Vendor>> GetAllAsync();
        Task<List<Vendor>> GetActiveVendorsAsync();
        Task<List<Vendor>> GetByVerificationStatusAsync(bool isVerified);
        Task UpdateAsync(Vendor vendor);
        Task DeleteAsync(int vendorId);
        Task<bool> ExistsByIdAsync(int vendorId);
        Task<bool> ExistsByUserIdAsync(int userId);
        
        // Related entities
        Task<List<WorkSchedule>> GetWorkSchedulesAsync(int vendorId);
        Task<List<DayOff>> GetDayOffsAsync(int vendorId);
        Task<List<VendorImage>> GetVendorImagesAsync(int vendorId);
        Task AddWorkScheduleAsync(WorkSchedule workSchedule);
        Task AddDayOffAsync(DayOff dayOff);
        Task AddVendorImageAsync(VendorImage vendorImage);
        Task<VendorRegisterRequest> GetVendorRegisterRequestAsync(int vendorId);
        Task<List<VendorRegisterRequest>> GetAllVendorRegisterRequestsAsync();
        Task AddVendorRegisterRequestAsync(VendorRegisterRequest request);
        Task UpdateVendorRegisterRequestAsync(VendorRegisterRequest request);
    }
}
