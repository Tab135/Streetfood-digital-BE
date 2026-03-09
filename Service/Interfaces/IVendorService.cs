using BO.Common;
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
        Task<PaginatedResponse<VendorResponseDto>> GetAllVendorsAsync(int pageNumber, int pageSize);
        Task<PaginatedResponse<VendorResponseDto>> GetActiveVendorsAsync(int pageNumber, int pageSize);
        Task DeleteVendorAsync(int vendorId);

        // Update
        Task<VendorResponseDto> UpdateVendorAsync(int userId, UpdateVendorDto updateVendorDto);

        // Admin operations
        Task<bool> SuspendVendorAsync(int vendorId);
        Task<bool> ReactivateVendorAsync(int vendorId);
    }
}
