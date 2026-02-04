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
        Task<(List<Vendor> items, int totalCount)> GetAllAsync(int pageNumber, int pageSize);
        Task<(List<Vendor> items, int totalCount)> GetActiveVendorsAsync(int pageNumber, int pageSize);
        Task UpdateAsync(Vendor vendor);
        Task DeleteAsync(int vendorId);
        Task<bool> ExistsByIdAsync(int vendorId);
        Task<bool> ExistsByUserIdAsync(int userId);
        
        // Related entities
        Task<List<VendorImage>> GetVendorImagesAsync(int vendorId);
        Task AddVendorImageAsync(VendorImage vendorImage);
        Task<VendorRegisterRequest> GetVendorRegisterRequestAsync(int vendorId);
        Task<List<VendorRegisterRequest>> GetAllVendorRegisterRequestsAsync();
        Task AddVendorRegisterRequestAsync(VendorRegisterRequest request);
        Task UpdateVendorRegisterRequestAsync(VendorRegisterRequest request);
    }
}
