using BO.DTO.AI;
using System.Collections.Generic;

namespace Service.Interfaces
{
    public interface IAiConversationMemoryService
    {
        List<AiChatHistoryMessageDto> GetHistory(int userId);
        void AddMessage(int userId, string role, string content);
        void ClearHistory(int userId);
    }
}
