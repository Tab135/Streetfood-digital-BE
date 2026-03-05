using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BO.DTO.Taste;
using BO.Entities;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service
{
    public class TasteService : ITasteService
    {
        private readonly ITasteRepository _repo;
        private readonly IVendorRepository _vendorRepository;
        private readonly IBranchRepository _branchRepository;

        public TasteService(
            ITasteRepository repo,
            IVendorRepository vendorRepository,
            IBranchRepository branchRepository)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _vendorRepository = vendorRepository ?? throw new ArgumentNullException(nameof(vendorRepository));
            _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
        }

        public async Task<TasteDto> CreateTasteAsync(CreateTasteDto createDto, int userId)
        {
            var entity = new Taste
            {
                Name = createDto.Name,
                Description = createDto.Description
            };

            var created = await _repo.CreateAsync(entity);
            return MapToDto(created);
        }

        public async Task<TasteDto?> GetTasteByIdAsync(int id)
        {
            var taste = await _repo.GetByIdAsync(id);
            return taste == null ? null : MapToDto(taste);
        }

        public async Task<List<TasteDto>> GetAllTastesAsync()
        {
            var list = await _repo.GetAllAsync();
            return list.Select(MapToDto).ToList();
        }

        public async Task<TasteDto> UpdateTasteAsync(int id, UpdateTasteDto updateDto, int userId)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new DomainExceptions($"Taste with id {id} not found");

            if (!string.IsNullOrEmpty(updateDto.Name))
                existing.Name = updateDto.Name;

            if (updateDto.Description != null)
                existing.Description = updateDto.Description;

            await _repo.UpdateAsync(existing);
            return MapToDto(existing);
        }

        public async Task<bool> DeleteTasteAsync(int id, int userId)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new DomainExceptions($"Taste with id {id} not found");

            await _repo.DeleteAsync(id);
            return true;
        }

        private static TasteDto MapToDto(Taste t)
        {
            return new TasteDto
            {
                TasteId = t.TasteId,
                Name = t.Name,
                Description = t.Description
            };
        }
    }
}
