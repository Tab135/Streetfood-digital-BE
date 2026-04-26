using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class FeedbackDAO
    {
        private readonly StreetFoodDbContext _context;

        public FeedbackDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Create feedback with images and tags
        public async Task<Feedback> CreateAsync(Feedback feedback, List<string> imageUrls = null, List<int> tagIds = null)
        {
            feedback.CreatedAt = DateTime.UtcNow;
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            // Add images if provided
            if (imageUrls != null && imageUrls.Any())
            {
                foreach (var url in imageUrls)
                {
                    var image = new FeedbackImage
                    {
                        FeedbackId = feedback.FeedbackId,
                        ImageUrl = url
                    };
                    _context.FeedbackImages.Add(image);
                }
                await _context.SaveChangesAsync();
            }

            // Add tag associations if provided
            if (tagIds != null && tagIds.Any())
            {
                foreach (var tagId in tagIds)
                {
                    var association = new FeedbackTagAssociation
                    {
                        FeedbackId = feedback.FeedbackId,
                        TagId = tagId
                    };
                    _context.FeedbackTagAssociations.Add(association);
                }
                await _context.SaveChangesAsync();
            }

            return await GetByIdAsync(feedback.FeedbackId);
        }

        // Get feedback by ID with all related data
        public async Task<Feedback> GetByIdAsync(int feedbackId)
        {
            return await _context.Feedbacks
                .Include(f => f.User)
                    .ThenInclude(u => u.UserBadges)
                        .ThenInclude(ub => ub.Badge)
                .Include(f => f.Branch)
                .Include(f => f.FeedbackImages)
                .Include(f => f.FeedbackTagAssociations)
                    .ThenInclude(fta => fta.FeedbackTag)
                .Include(f => f.VendorReply)
                    .ThenInclude(r => r.User)
                .Include(f => f.Votes)
                .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId);
        }

        // Add multiple images to existing feedback
        public async Task AddImagesToFeedbackAsync(int feedbackId, List<string> imageUrls)
        {
            if (imageUrls != null && imageUrls.Any())
            {
                foreach (var url in imageUrls)
                {
                    var image = new FeedbackImage
                    {
                        FeedbackId = feedbackId,
                        ImageUrl = url
                    };
                    _context.FeedbackImages.Add(image);
                }
                await _context.SaveChangesAsync();
            }
        }

        // Get feedback by branch ID with sorting
        public async Task<(List<Feedback> items, int totalCount)> GetByBranchIdAsync(
            int branchId, int pageNumber, int pageSize, string? sortBy = null)
        {
            var query = _context.Feedbacks
                .Where(f => f.BranchId == branchId);

            var totalCount = await query.CountAsync();

            var orderedQuery = query
                .Include(f => f.User)
                    .ThenInclude(u => u.UserBadges)
                        .ThenInclude(ub => ub.Badge)
                .Include(f => f.FeedbackImages)
                .Include(f => f.FeedbackTagAssociations)
                    .ThenInclude(fta => fta.FeedbackTag)
                .Include(f => f.VendorReply)
                    .ThenInclude(r => r.User)
                .Include(f => f.Votes)
                .AsQueryable();

            orderedQuery = sortBy switch
            {
                "most_helpful" => orderedQuery
                    .OrderByDescending(f => f.Votes.Count(v => v.VoteType == VoteType.Up) -
                                            f.Votes.Count(v => v.VoteType == VoteType.Down))
                    .ThenByDescending(f => f.CreatedAt),
                "highest_rating" => orderedQuery
                    .OrderByDescending(f => f.Rating)
                    .ThenByDescending(f => f.CreatedAt),
                "lowest_rating" => orderedQuery
                    .OrderBy(f => f.Rating)
                    .ThenByDescending(f => f.CreatedAt),
                _ => orderedQuery // "newest" or default
                    .OrderByDescending(f => f.CreatedAt)
            };

            var items = await orderedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // Get feedback by user ID
        public async Task<(List<Feedback> items, int totalCount)> GetByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            var query = _context.Feedbacks
                .Where(f => f.UserId == userId);

            var totalCount = await query.CountAsync();
            
            var items = await query
                .Include(f => f.Branch)
                .Include(f => f.FeedbackImages)
                .Include(f => f.FeedbackTagAssociations)
                    .ThenInclude(fta => fta.FeedbackTag)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // Get average rating for a branch
        public async Task<double> GetAverageRatingByBranchIdAsync(int branchId)
        {
            var feedbacks = await _context.Feedbacks
                .Where(f => f.BranchId == branchId)
                .ToListAsync();

            if (!feedbacks.Any()) return 0;
            return feedbacks.Average(f => f.Rating);
        }

        // Get feedback count by branch
        public async Task<int> GetCountByBranchIdAsync(int branchId)
        {
            return await _context.Feedbacks
                .Where(f => f.BranchId == branchId)
                .CountAsync();
        }

        public async Task<Dictionary<int, int>> GetFeedbackCountByStarsAsync(int branchId)
        {
            return await _context.Feedbacks
                .Where(f => f.BranchId == branchId)
                .GroupBy(f => f.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Rating, x => x.Count);
        }

        public async Task<int?> GetRatingOfRecentFeedbackAsync(int branchId, int offset)
        {
            return await _context.Feedbacks
                .Where(f => f.BranchId == branchId)
                .OrderByDescending(f => f.CreatedAt)
                .Skip(offset)
                .Select(f => (int?)f.Rating)
                .FirstOrDefaultAsync();
        }

        // Update feedback
        public async Task<Feedback> UpdateAsync(Feedback feedback)
        {
            feedback.UpdatedAt = DateTime.UtcNow;
            _context.Feedbacks.Update(feedback);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(feedback.FeedbackId);
        }

        // Delete feedback (will cascade delete images and tag associations)
        public async Task<bool> DeleteAsync(int feedbackId)
        {
            var feedback = await _context.Feedbacks.FindAsync(feedbackId);
            if (feedback == null) return false;

            _context.Feedbacks.Remove(feedback);
            await _context.SaveChangesAsync();
            return true;
        }

        // Check if feedback exists
        public async Task<bool> ExistsAsync(int feedbackId)
        {
            return await _context.Feedbacks.AnyAsync(f => f.FeedbackId == feedbackId);
        }

        // Check if user has feedback on a branch
        public async Task<bool> HasUserFeedbackOnBranchAsync(int branchId, int userId)
        {
            return await _context.Feedbacks
                .AnyAsync(f => f.BranchId == branchId && f.UserId == userId);
        }

        public async Task<bool> HasUserFeedbackOnBranchWithoutOrderAsync(int branchId, int userId)
        {
            return await _context.Feedbacks
                .AnyAsync(f => f.BranchId == branchId && f.UserId == userId && f.OrderId == null);
        }

        // Check if user already left feedback for an order
        public async Task<bool> HasFeedbackForOrderAsync(int userId, int orderId)
        {
            return await _context.Feedbacks
                .AnyAsync(f => f.UserId == userId && f.OrderId == orderId);
        }

        // ===== Feedback Image Management =====
        
        public async Task<FeedbackImage> AddImageAsync(FeedbackImage image)
        {
            _context.FeedbackImages.Add(image);
            await _context.SaveChangesAsync();
            return image;
        }

        public async Task<List<FeedbackImage>> GetImagesByFeedbackIdAsync(int feedbackId)
        {
            return await _context.FeedbackImages
                .Where(fi => fi.FeedbackId == feedbackId)
                .ToListAsync();
        }

        public async Task<bool> DeleteImageAsync(int imageId)
        {
            var image = await _context.FeedbackImages.FindAsync(imageId);
            if (image == null) return false;

            _context.FeedbackImages.Remove(image);
            await _context.SaveChangesAsync();
            return true;
        }

        // ===== Feedback Tag Association Management =====
        
        public async Task<FeedbackTagAssociation> AddTagAsync(int feedbackId, int tagId)
        {
            var association = new FeedbackTagAssociation
            {
                FeedbackId = feedbackId,
                TagId = tagId
            };
            _context.FeedbackTagAssociations.Add(association);
            await _context.SaveChangesAsync();
            return association;
        }

        public async Task<List<FeedbackTagAssociation>> GetTagsByFeedbackIdAsync(int feedbackId)
        {
            return await _context.FeedbackTagAssociations
                .Where(fta => fta.FeedbackId == feedbackId)
                .Include(fta => fta.FeedbackTag)
                .ToListAsync();
        }

        public async Task<bool> RemoveTagAsync(int feedbackId, int tagId)
        {
            var association = await _context.FeedbackTagAssociations
                .FirstOrDefaultAsync(fta => fta.FeedbackId == feedbackId && fta.TagId == tagId);
            if (association == null) return false;

            _context.FeedbackTagAssociations.Remove(association);
            await _context.SaveChangesAsync();
            return true;
        }

        // Remove all tags for a feedback
        public async Task RemoveAllTagsAsync(int feedbackId)
        {
            var associations = await _context.FeedbackTagAssociations
                .Where(fta => fta.FeedbackId == feedbackId)
                .ToListAsync();

            _context.FeedbackTagAssociations.RemoveRange(associations);
            await _context.SaveChangesAsync();
        }

        // Get feedback by rating range
        public async Task<(List<Feedback> items, int totalCount)> GetByRatingRangeAsync(
            int branchId, int minRating, int maxRating, int pageNumber, int pageSize)
        {
            var query = _context.Feedbacks
                .Where(f => f.BranchId == branchId && f.Rating >= minRating && f.Rating <= maxRating);

            var totalCount = await query.CountAsync();
            
            var items = await query
                .Include(f => f.User)
                    .ThenInclude(u => u.UserBadges)
                        .ThenInclude(ub => ub.Badge)
                .Include(f => f.FeedbackImages)
                .Include(f => f.FeedbackTagAssociations)
                    .ThenInclude(fta => fta.FeedbackTag)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // Velocity Limits
        public async Task<int> GetDailyFeedbackCountAsync(int userId, DateTime date)
        {
            return await _context.Feedbacks
                .Where(f => f.UserId == userId && f.CreatedAt.Date == date.Date)
                .CountAsync();
        }

        public async Task<int> GetDailyFeedbackCountWithoutOrderAsync(int userId, DateTime date)
        {
            return await _context.Feedbacks
                .Where(f => f.UserId == userId && f.OrderId == null && f.CreatedAt.Date == date.Date)
                .CountAsync();
        }

        public async Task<List<int>> GetReviewedBranchIdsTodayAsync(int userId, DateTime date)
        {
            return await _context.Feedbacks
                .Where(f => f.UserId == userId && f.CreatedAt.Date == date.Date)
                .Select(f => f.BranchId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<int>> GetReviewedBranchIdsTodayWithoutOrderAsync(int userId, DateTime date)
        {
            return await _context.Feedbacks
                .Where(f => f.UserId == userId && f.OrderId == null && f.CreatedAt.Date == date.Date)
                .Select(f => f.BranchId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool> HasReviewedBranchTodayAsync(int userId, int branchId, DateTime date)
        {
            return await _context.Feedbacks
                .AnyAsync(f => f.UserId == userId && f.BranchId == branchId && f.CreatedAt.Date == date.Date);
        }
    }
}
