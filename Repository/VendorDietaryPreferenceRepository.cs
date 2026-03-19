using BO.Entities;
using DAL;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class VendorDietaryPreferenceRepository : IVendorDietaryPreferenceRepository
    {
        private readonly VendorDietaryPreferenceDAO _dao;

        public VendorDietaryPreferenceRepository(VendorDietaryPreferenceDAO dao)
        {
            _dao = dao ?? throw new ArgumentNullException(nameof(dao));
        }

        public async Task<List<DietaryPreference>> GetPreferencesByVendorId(int vendorId)
        {
            return await _dao.GetPreferencesByVendorId(vendorId);
        }

        public async Task AssignPreferencesToVendor(int vendorId, List<int> dietaryPreferenceIds)
        {
            await _dao.AssignPreferencesToVendor(vendorId, dietaryPreferenceIds);
        }
    }
}
