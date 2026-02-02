using BO.DTO.Vendor;
using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IVendorService
    {
        // CRUD operations
        Task<Vendor> CreateVendorAsync(CreateVendorDto createVendorDto, int userId);
        Task<VendorResponseDto> GetVendorByIdAsync(int vendorId);
        Task<VendorResponseDto> GetVendorByUserIdAsync(int userId);
        Task<List<VendorResponseDto>> GetAllVendorsAsync();
        Task<List<VendorResponseDto>> GetActiveVendorsAsync();
        Task<List<VendorResponseDto>> GetUnverifiedVendorsAsync();
        Task<Vendor> UpdateVendorAsync(int vendorId, UpdateVendorDto updateVendorDto);
        Task DeleteVendorAsync(int vendorId);

        // Vendor registration with license
        Task<VendorRegisterRequest> SubmitVendorRegistrationAsync(int vendorId, string licenseImagePath);
        Task<VendorRegisterRequest> GetVendorRegistrationStatusAsync(int vendorId);
        Task<List<VendorRegisterRequest>> GetPendingVendorRegistrationsAsync();

        // Schedule and day off management
        Task AddWorkScheduleAsync(int vendorId, AddWorkScheduleDto scheduleDto);
        Task AddDayOffAsync(int vendorId, AddDayOffDto dayOffDto);
        Task<List<WorkSchedule>> GetVendorSchedulesAsync(int vendorId);
        Task<List<DayOff>> GetVendorDayOffsAsync(int vendorId);

        // Vendor images
        Task AddVendorImageAsync(int vendorId, string imageUrl);
        Task<List<VendorImage>> GetVendorImagesAsync(int vendorId);

        // Admin operations
        Task<bool> VerifyVendorAsync(int vendorId);
        Task<bool> RejectVendorRegistrationAsync(int vendorId, string rejectionReason);
        Task<bool> SuspendVendorAsync(int vendorId);
        Task<bool> ReactivateVendorAsync(int vendorId);
    }
}
