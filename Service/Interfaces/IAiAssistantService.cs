using BO.DTO.AI;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IAiAssistantService
    {
        Task<AiChatResponseDto> ChatAsync(int userId, AiChatRequestDto request, IFormFile? image = null);
    }
}
