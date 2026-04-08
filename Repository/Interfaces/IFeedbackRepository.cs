using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IFeedbackRepository
    {
        // Feedback CRUD
        Task<Feedback> Create(Feedback feedback, List<string> imageUrls = null, List<int> tagIds = null);
        Task<Feedback> GetById(int feedbackId);
        Task<(List<Feedback> items, int totalCount)> GetByBranchId(int branchId, int pageNumber, int pageSize, string? sortBy = null);
        Task<(List<Feedback> items, int totalCount)> GetByUserId(int userId, int pageNumber, int pageSize);
        Task<Feedback> Update(Feedback feedback);
        Task<bool> Delete(int feedbackId);
        Task<bool> Exists(int feedbackId);
        Task<bool> HasUserFeedbackOnBranch(int branchId, int userId);
        Task<bool> HasFeedbackForOrder(int userId, int orderId);

        // Rating and Statistics
        Task<double> GetAverageRatingByBranchId(int branchId);
        Task<int> GetCountByBranchId(int branchId);
        Task<Dictionary<int, int>> GetFeedbackCountByStarsAsync(int branchId);
        Task<int?> GetRatingOfRecentFeedbackAsync(int branchId, int offset);
        Task<(List<Feedback> items, int totalCount)> GetByRatingRange(
            int branchId, int minRating, int maxRating, int pageNumber, int pageSize);

        // Image Management
        Task<FeedbackImage> AddImage(FeedbackImage image);
        Task<List<FeedbackImage>> GetImagesByFeedbackId(int feedbackId);
        Task<bool> DeleteImage(int imageId);
        Task AddImagesToFeedback(int feedbackId, List<string> imageUrls);

        // Tag Management
        Task<FeedbackTagAssociation> AddTag(int feedbackId, int tagId);
        Task<List<FeedbackTagAssociation>> GetTagsByFeedbackId(int feedbackId);
        Task<bool> RemoveTag(int feedbackId, int tagId);
        Task RemoveAllTags(int feedbackId);

        // Velocity Limits
        Task<int> GetDailyFeedbackCountAsync(int userId, DateTime date);
        Task<List<int>> GetReviewedBranchIdsTodayAsync(int userId, DateTime date);
        Task<bool> HasReviewedBranchTodayAsync(int userId, int branchId, DateTime date);
    }
}
