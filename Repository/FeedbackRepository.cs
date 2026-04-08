using BO.Entities;
using DAL;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly FeedbackDAO _feedbackDAO;

        public FeedbackRepository(FeedbackDAO feedbackDAO)
        {
            _feedbackDAO = feedbackDAO ?? throw new ArgumentNullException(nameof(feedbackDAO));
        }

        // Feedback CRUD Operations
        public async Task<Feedback> Create(Feedback feedback, List<string> imageUrls = null, List<int> tagIds = null)
        {
            return await _feedbackDAO.CreateAsync(feedback, imageUrls, tagIds);
        }

        public async Task<Feedback> GetById(int feedbackId)
        {
            return await _feedbackDAO.GetByIdAsync(feedbackId);
        }

        public async Task<(List<Feedback> items, int totalCount)> GetByBranchId(
            int branchId, int pageNumber, int pageSize, string? sortBy = null)
        {
            return await _feedbackDAO.GetByBranchIdAsync(branchId, pageNumber, pageSize, sortBy);
        }

        public async Task<(List<Feedback> items, int totalCount)> GetByUserId(int userId, int pageNumber, int pageSize)
        {
            return await _feedbackDAO.GetByUserIdAsync(userId, pageNumber, pageSize);
        }

        public async Task<Feedback> Update(Feedback feedback)
        {
            return await _feedbackDAO.UpdateAsync(feedback);
        }

        public async Task<bool> Delete(int feedbackId)
        {
            return await _feedbackDAO.DeleteAsync(feedbackId);
        }

        public async Task<bool> Exists(int feedbackId)
        {
            return await _feedbackDAO.ExistsAsync(feedbackId);
        }

        public async Task<bool> HasUserFeedbackOnBranch(int branchId, int userId)
        {
            return await _feedbackDAO.HasUserFeedbackOnBranchAsync(branchId, userId);
        }

        public async Task<bool> HasFeedbackForOrder(int userId, int orderId)
        {
            return await _feedbackDAO.HasFeedbackForOrderAsync(userId, orderId);
        }

        // Rating and Statistics
        public async Task<double> GetAverageRatingByBranchId(int branchId)
        {
            return await _feedbackDAO.GetAverageRatingByBranchIdAsync(branchId);
        }

        public async Task<int> GetCountByBranchId(int branchId)
        {
            return await _feedbackDAO.GetCountByBranchIdAsync(branchId);
        }

        public async Task<Dictionary<int, int>> GetFeedbackCountByStarsAsync(int branchId)
        {
            return await _feedbackDAO.GetFeedbackCountByStarsAsync(branchId);
        }

        public async Task<int?> GetRatingOfRecentFeedbackAsync(int branchId, int offset)
        {
            return await _feedbackDAO.GetRatingOfRecentFeedbackAsync(branchId, offset);
        }

        public async Task<(List<Feedback> items, int totalCount)> GetByRatingRange(
            int branchId, int minRating, int maxRating, int pageNumber, int pageSize)
        {
            return await _feedbackDAO.GetByRatingRangeAsync(branchId, minRating, maxRating, pageNumber, pageSize);
        }

        // Image Management
        public async Task<FeedbackImage> AddImage(FeedbackImage image)
        {
            return await _feedbackDAO.AddImageAsync(image);
        }

        public async Task<List<FeedbackImage>> GetImagesByFeedbackId(int feedbackId)
        {
            return await _feedbackDAO.GetImagesByFeedbackIdAsync(feedbackId);
        }

        public async Task<bool> DeleteImage(int imageId)
        {
            return await _feedbackDAO.DeleteImageAsync(imageId);
        }

        public async Task AddImagesToFeedback(int feedbackId, List<string> imageUrls)
        {
            await _feedbackDAO.AddImagesToFeedbackAsync(feedbackId, imageUrls);
        }

        // Tag Management
        public async Task<FeedbackTagAssociation> AddTag(int feedbackId, int tagId)
        {
            return await _feedbackDAO.AddTagAsync(feedbackId, tagId);
        }

        public async Task<List<FeedbackTagAssociation>> GetTagsByFeedbackId(int feedbackId)
        {
            return await _feedbackDAO.GetTagsByFeedbackIdAsync(feedbackId);
        }

        public async Task<bool> RemoveTag(int feedbackId, int tagId)
        {
            return await _feedbackDAO.RemoveTagAsync(feedbackId, tagId);
        }

        public async Task RemoveAllTags(int feedbackId)
        {
            await _feedbackDAO.RemoveAllTagsAsync(feedbackId);
        }

        // Velocity Limits
        public async Task<int> GetDailyFeedbackCountAsync(int userId, DateTime date)
        {
            return await _feedbackDAO.GetDailyFeedbackCountAsync(userId, date);
        }

        public async Task<List<int>> GetReviewedBranchIdsTodayAsync(int userId, DateTime date)
        {
            return await _feedbackDAO.GetReviewedBranchIdsTodayAsync(userId, date);
        }

        public async Task<bool> HasReviewedBranchTodayAsync(int userId, int branchId, DateTime date)
        {
            return await _feedbackDAO.HasReviewedBranchTodayAsync(userId, branchId, date);
        }
    }
}
