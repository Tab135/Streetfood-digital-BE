using BO.Common;
using BO.DTO.Feedback;
using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IFeedbackService
    {
        // Feedback CRUD
        Task<FeedbackResponseDto> CreateFeedback(CreateFeedbackDto createFeedbackDto, int userId);
        Task<FeedbackResponseDto> GetFeedbackById(int feedbackId);
        Task<FeedbackResponseDto> AddImagesToFeedback(int feedbackId, List<string> imageUrls, int userId);
        Task<PaginatedResponse<FeedbackResponseDto>> GetFeedbackByBranchId(int branchId, int pageNumber, int pageSize, string? sortBy = null, int? currentUserId = null);
        Task<PaginatedResponse<FeedbackResponseDto>> GetFeedbackByUserId(int userId, int pageNumber, int pageSize);
        Task<FeedbackResponseDto> UpdateFeedback(int feedbackId, UpdateFeedbackDto updateFeedbackDto, int userId);
        Task<bool> DeleteFeedback(int feedbackId, int userId);

        // Rating and Statistics
        Task<double> GetAverageRatingByBranch(int branchId);
        Task<int> GetFeedbackCountByBranch(int branchId);
        Task<PaginatedResponse<FeedbackResponseDto>> GetFeedbackByRatingRange(
            int branchId, int minRating, int maxRating, int pageNumber, int pageSize);

        // Image Reference (for viewing)
        Task<List<FeedbackImageDto>> GetFeedbackImages(int feedbackId);
    }
}
