using BO.DTO.Tier;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface ITierService
    {
        Task<List<TierResponseDto>> GetAllTiersAsync();
        Task<TierResponseDto> GetByIdAsync(int tierId);
    }
}