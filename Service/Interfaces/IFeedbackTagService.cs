using BO.DTO.FeedbackTag;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IFeedbackTagService
    {
        Task<FeedbackTagDto> CreateFeedbackTag(CreateFeedbackTagDto createDto);
        Task<FeedbackTagDto> UpdateFeedbackTag(int id, UpdateFeedbackTagDto updateDto);
        Task<bool> DeleteFeedbackTag(int id);
        Task<List<FeedbackTagDto>> GetAllFeedbackTags();
        Task<FeedbackTagDto?> GetFeedbackTagById(int id);
    }
}