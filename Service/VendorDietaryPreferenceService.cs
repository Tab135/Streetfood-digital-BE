using BO.DTO.Dietary;
using BO.Entities;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service
{
    public class VendorDietaryPreferenceService : IVendorDietaryPreferenceService
    {
        private readonly IVendorDietaryPreferenceRepository _repository;
        private readonly IVendorRepository _vendorRepository;

        public VendorDietaryPreferenceService(
            IVendorDietaryPreferenceRepository repository,
            IVendorRepository vendorRepository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _vendorRepository = vendorRepository ?? throw new ArgumentNullException(nameof(vendorRepository));
        }

        public async Task<List<DietaryPreferenceDto>> GetPreferencesByVendorId(int vendorId)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
                throw new DomainExceptions($"Không tìm thấy cửa hàng với ID {vendorId}");

            var prefs = await _repository.GetPreferencesByVendorId(vendorId);
            return prefs.Select(MapToDto).ToList();
        }

        public async Task<List<DietaryPreferenceDto>> AssignPreferencesToVendor(int vendorId, List<int> dietaryPreferenceIds)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
                throw new DomainExceptions($"Không tìm thấy cửa hàng với ID {vendorId}");

            await _repository.AssignPreferencesToVendor(vendorId, dietaryPreferenceIds);
            var prefs = await _repository.GetPreferencesByVendorId(vendorId);
            return prefs.Select(MapToDto).ToList();
        }

        private static DietaryPreferenceDto MapToDto(DietaryPreference dp) => new DietaryPreferenceDto
        {
            DietaryPreferenceId = dp.DietaryPreferenceId,
            Name = dp.Name,
            Description = dp.Description
        };
    }
}
