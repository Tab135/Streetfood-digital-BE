using BO.DTO.Taste;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface ITasteService
    {
        Task<TasteDto> CreateTasteAsync(CreateTasteDto createDto, int userId);
        Task<TasteDto> UpdateTasteAsync(int id, UpdateTasteDto updateDto, int userId);
        Task<bool> DeleteTasteAsync(int id, int userId);
        Task<List<TasteDto>> GetAllTastesAsync();
        Task<TasteDto?> GetTasteByIdAsync(int id);
    }
}
