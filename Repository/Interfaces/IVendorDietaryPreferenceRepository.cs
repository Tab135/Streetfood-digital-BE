using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IVendorDietaryPreferenceRepository
    {
        Task<List<DietaryPreference>> GetPreferencesByVendorId(int vendorId);
        Task AssignPreferencesToVendor(int vendorId, List<int> dietaryPreferenceIds);
    }
}
