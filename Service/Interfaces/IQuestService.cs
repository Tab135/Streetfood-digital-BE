using BO.Common;
using BO.DTO.Quest;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IQuestService
    {
        Task<QuestResponseDto> CreateQuestAsync(CreateQuestDto dto);
        Task<QuestResponseDto> UpdateQuestAsync(int questId, UpdateQuestDto dto);
        Task<QuestResponseDto> ReplaceQuestTasksAsync(int questId, List<CreateQuestTaskDto> tasks);
        Task<bool> DeleteQuestAsync(int questId);
        Task<QuestResponseDto?> GetQuestByIdAsync(int questId);
        Task<QuestTaskResponseDto?> GetQuestTaskByIdAsync(int questTaskId);
        Task<PaginatedResponse<QuestResponseDto>> GetQuestsAsync(QuestQueryDto query);
        Task<PaginatedResponse<QuestResponseDto>> GetPublicQuestsAsync(QuestQueryDto query);
        Task<UserQuestProgressDto> EnrollInQuestAsync(int userId, int questId);
        Task<UserQuestProgressDto> StopQuestAsync(int userId, int questId);
        Task<PaginatedResponse<UserQuestProgressDto>> GetMyQuestsAsync(int userId, string? status, bool? isTierUp = null, int pageNumber = 1, int pageSize = 10);
        Task<System.Collections.Generic.List<UserQuestProgressDto>> GetCampaignQuestProgressAsync(int userId, int campaignId);
        Task<QuestResponseDto> UpdateQuestImageAsync(int questId, string imageUrl);
    }
}
