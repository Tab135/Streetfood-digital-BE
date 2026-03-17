using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BO.DTO.Tier;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service
{
    public class TierService : ITierService
    {
        private readonly ITierRepository _tierRepository;

        public TierService(ITierRepository tierRepository)
        {
            _tierRepository = tierRepository;
        }

        public async Task<List<TierResponseDto>> GetAllTiersAsync()
        {
            var tiers = await _tierRepository.GetAllAsync();
            return tiers.Select(t => new TierResponseDto
            {
                TierId = t.TierId,
                Name = t.Name,
                Weight = t.Weight
            }).ToList();
        }
    }
}