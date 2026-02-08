using BO.DTO.Dietary;
using BO.Entities;
using Repository.Interfaces;
using Service.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service
{
    public class DietaryPreferenceService : IDietaryPreferenceService
    {
        private readonly IDietaryPreferenceRepository _repo;

        public DietaryPreferenceService(IDietaryPreferenceRepository repo)
        {
            _repo = repo;
        }

        public async Task<DietaryPreferenceDto> CreateDietaryPreference(CreateDietaryPreferenceDto createDto)
        {
            var entity = new DietaryPreference
            {
                Name = createDto.Name,
                Description = createDto.Description
            };

            var created = await _repo.Create(entity);
            return MapToDto(created);
        }

        public async Task<bool> DeleteDietaryPreference(int id)
        {
            var exists = await _repo.Exists(id);
            if (!exists) throw new System.Exception($"Dietary preference with id {id} not found");
            return await _repo.Delete(id);
        }

        public async Task<List<DietaryPreferenceDto>> GetAllDietaryPreferences()
        {
            var list = await _repo.GetAll();
            return list.Select(MapToDto).ToList();
        }

        public async Task<DietaryPreferenceDto?> GetDietaryPreferenceById(int id)
        {
            var d = await _repo.GetById(id);
            return d == null ? null : MapToDto(d);
        }

        public async Task<DietaryPreferenceDto> UpdateDietaryPreference(int id, UpdateDietaryPreferenceDto updateDto)
        {
            var existing = await _repo.GetById(id);
            if (existing == null) throw new System.Exception($"Dietary preference with id {id} not found");

            if (!string.IsNullOrEmpty(updateDto.Name)) existing.Name = updateDto.Name;
            if (updateDto.Description != null) existing.Description = updateDto.Description;

            var updated = await _repo.Update(existing);
            return MapToDto(updated);
        }

        private DietaryPreferenceDto MapToDto(DietaryPreference d)
        {
            return new DietaryPreferenceDto
            {
                DietaryPreferenceId = d.DietaryPreferenceId,
                Name = d.Name,
                Description = d.Description
            };
        }
    }
}