using BO.DTO.AI;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IAiAssistantService
    {
        Task<AiChatResponseDto> ChatAsync(int userId, AiChatRequestDto request);
    }
}
