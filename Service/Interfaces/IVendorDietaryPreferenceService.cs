using BO.DTO.Dietary;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IVendorDietaryPreferenceService
    {
        Task<List<DietaryPreferenceDto>> GetPreferencesByVendorId(int vendorId);
        Task<List<DietaryPreferenceDto>> AssignPreferencesToVendor(int vendorId, List<int> dietaryPreferenceIds);
    }
}
