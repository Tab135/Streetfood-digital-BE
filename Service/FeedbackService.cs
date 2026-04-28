using BO.Common;
using BO.DTO.Feedback;
using BO.Entities;
using BO.Enums;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service
{
    public class FeedbackService : IFeedbackService
    {
        private const string FeedbackDailyLimitSettingName = "feedbackDailyLimit";
        private const double MaxDistanceWithoutOrderKm = 0.3;

        private readonly IFeedbackRepository _feedbackRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IFeedbackTagRepository _feedbackTagRepository;
        private readonly IDishRepository _dishRepository;
        private readonly IBranchMetricsService _branchMetricsService;
        private readonly INotificationService _notificationService;
        private readonly IOrderRepository _orderRepository;
        private readonly IQuestProgressService _questProgressService;
        private readonly ISettingService _settingService;
        private readonly IUserService _userService;

        public FeedbackService(
            IFeedbackRepository feedbackRepository,
            IUserRepository userRepository,
            IBranchRepository branchRepository,
            IFeedbackTagRepository feedbackTagRepository,
            IDishRepository dishRepository,
            IBranchMetricsService branchMetricsService,
            INotificationService notificationService,
            IOrderRepository orderRepository,
            IQuestProgressService questProgressService,
            ISettingService settingService,
            IUserService userService)
        {
            _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
            _feedbackTagRepository = feedbackTagRepository ?? throw new ArgumentNullException(nameof(feedbackTagRepository));
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _branchMetricsService = branchMetricsService ?? throw new ArgumentNullException(nameof(branchMetricsService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _questProgressService = questProgressService ?? throw new ArgumentNullException(nameof(questProgressService));
            _settingService = settingService ?? throw new ArgumentNullException(nameof(settingService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        public async Task<FeedbackResponseDto> CreateFeedback(CreateFeedbackDto createFeedbackDto, int userId)
        {
            var today = DateTime.UtcNow.Date;
            var dailyLimitWithoutOrder = _settingService.GetInt(FeedbackDailyLimitSettingName, 3);

            // Verify user exists
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
            {
                throw new DomainExceptions($"Không tìm thấy người dùng với ID {userId}");
            }

            // Verify branch exists
            var branch = await _branchRepository.GetByIdAsync(createFeedbackDto.BranchId);
            if (branch == null)
            {
                throw new DomainExceptions($"Không tìm thấy chi nhánh với ID {createFeedbackDto.BranchId}");
            }

            // If DishId is provided, verify it exists and belongs to the branch
            if (createFeedbackDto.DishId.HasValue)
            {
                var dish = await _dishRepository.GetByIdAsync(createFeedbackDto.DishId.Value);
                if (dish == null)
                {
                    throw new DomainExceptions($"Không tìm thấy món ăn với ID {createFeedbackDto.DishId.Value}");
                }

                // Validate dish belongs to the specified branch
                var branchDish = await _dishRepository.GetBranchDishAsync(createFeedbackDto.BranchId, createFeedbackDto.DishId.Value);
                if (branchDish == null)
                {
                    throw new DomainExceptions($"Món ăn với ID {createFeedbackDto.DishId.Value} không thuộc chi nhánh với ID {createFeedbackDto.BranchId}");
                }
            }

            // Validate order if provided
            if (createFeedbackDto.OrderId.HasValue)
            {
                var order = await _orderRepository.GetById(createFeedbackDto.OrderId.Value);
                if (order == null)
                    throw new DomainExceptions("Không tìm thấy đơn hàng");
                if (order.UserId != userId)
                    throw new DomainExceptions("Đơn hàng không thuộc về người dùng này");
                if (order.BranchId != createFeedbackDto.BranchId)
                    throw new DomainExceptions("Đơn hàng không thuộc chi nhánh này");
                if (order.Status != OrderStatus.Complete)
                    throw new DomainExceptions("Đơn hàng phải hoàn thành trước khi đánh giá");

                // One review per order
                var hasFeedback = await _feedbackRepository.HasFeedbackForOrder(userId, createFeedbackDto.OrderId.Value);
                if (hasFeedback)
                    throw new DomainExceptions("Bạn đã đánh giá đơn hàng này rồi");
            }
            else
            {
                if (!createFeedbackDto.UserLat.HasValue || !createFeedbackDto.UserLong.HasValue)
                {
                    throw new DomainExceptions("Thiếu vị trí hiện tại. Vui lòng bật GPS để đánh giá khi không có đơn hàng.");
                }

                var distanceKm = HaversineDistance(
                    createFeedbackDto.UserLat.Value,
                    createFeedbackDto.UserLong.Value,
                    branch.Lat,
                    branch.Long);

                if (distanceKm > MaxDistanceWithoutOrderKm)
                {
                    var distanceMeters = (int)Math.Round(distanceKm * 1000);
                    throw new DomainExceptions($"Bạn đang cách quán khoảng {distanceMeters}m. Đánh giá không đơn hàng chỉ hợp lệ trong phạm vi 300m.");
                }

                var hasNonOrderFeedbackForBranch = await _feedbackRepository.HasUserFeedbackOnBranchWithoutOrderAsync(createFeedbackDto.BranchId, userId);
                if (hasNonOrderFeedbackForBranch)
                {
                    throw new DomainExceptions("Mỗi quán chỉ được đánh giá 1 lần khi không có đơn hàng.");
                }

                var reviewedBranchIdsToday = await _feedbackRepository.GetReviewedBranchIdsTodayWithoutOrderAsync(userId, today);
                if (reviewedBranchIdsToday.Count >= dailyLimitWithoutOrder)
                {
                    throw new DomainExceptions($"Bạn đã đạt giới hạn {dailyLimitWithoutOrder} quán khác nhau trong ngày hôm nay cho đánh giá không đơn hàng.");
                }
            }

            // Validate rating
            if (createFeedbackDto.Rating < 1 || createFeedbackDto.Rating > 5)
            {
                throw new DomainExceptions("Điểm đánh giá phải nằm trong khoảng từ 1 đến 5");
            }

            var feedbackXP = _settingService.GetInt("feedbackXP", 0);

            var feedback = new Feedback
            {
                UserId = userId,
                BranchId = createFeedbackDto.BranchId,
                DishId = createFeedbackDto.DishId,
                OrderId = createFeedbackDto.OrderId,
                Rating = createFeedbackDto.Rating,
                Comment = createFeedbackDto.Comment,
                FeedbackXP = feedbackXP > 0 ? feedbackXP : null,
                CreatedAt = DateTime.UtcNow
            };

            var createdFeedback = await _feedbackRepository.Create(
                feedback,
                null,
                createFeedbackDto.TagIds);

            // Award XP from config
            if (feedbackXP > 0)
            {
                await _userService.AddXPAsync(userId, feedbackXP);
            }

            // Recalculate branch metrics
            await _branchMetricsService.OnFeedbackCreated(createFeedbackDto.BranchId, createFeedbackDto.Rating);

            // Notify vendor
            if (branch != null)
            {
                int? recipientId = branch.ManagerId;
                if (recipientId.HasValue)
                {
                    await _notificationService.NotifyAsync(
                        recipientId.Value,
                        NotificationType.NewFeedback,
                        "Đánh giá mới",
                        $"Một đánh giá {createFeedbackDto.Rating}-sao trên chi nhánh của bạn '{branch.Name}'",
                        createdFeedback.FeedbackId);
                }

                // Update quest progress for REVIEW tasks
                if (branch.IsSubscribed)
                {
                await _questProgressService.UpdateProgressAsync(userId, QuestTaskType.REVIEW, 1);
                }
            }

         

            return await MapToResponseDtoAsync(createdFeedback);
        }

        public async Task<FeedbackResponseDto> AddImagesToFeedback(int feedbackId, List<string> imageUrls, int userId)
        {
            var feedback = await _feedbackRepository.GetById(feedbackId);
            if (feedback == null)
            {
                throw new DomainExceptions($"Không tìm thấy đánh giá với ID {feedbackId}");
            }

            // Verify the user owns this feedback
            if (feedback.UserId != userId)
            {
                throw new DomainExceptions("Bạn chỉ có thể thêm ảnh vào đánh giá của chính mình");
            }

            // Add images to feedback
            await _feedbackRepository.AddImagesToFeedback(feedbackId, imageUrls);

            // Return updated feedback
            var updatedFeedback = await _feedbackRepository.GetById(feedbackId);
            return await MapToResponseDtoAsync(updatedFeedback);
        }

        public async Task<FeedbackResponseDto> GetFeedbackById(int feedbackId)
        {
            var feedback = await _feedbackRepository.GetById(feedbackId);
            if (feedback == null)
            {
                throw new DomainExceptions($"Không tìm thấy đánh giá với ID {feedbackId}");
            }

            return await MapToResponseDtoAsync(feedback);
        }

        public async Task<PaginatedResponse<FeedbackResponseDto>> GetFeedbackByBranchId(
            int branchId, int pageNumber, int pageSize, string? sortBy = null, int? currentUserId = null)
        {
            // Verify branch exists
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
            {
                throw new DomainExceptions($"Không tìm thấy chi nhánh với ID {branchId}");
            }

            var (feedbacks, totalCount) = await _feedbackRepository.GetByBranchId(branchId, pageNumber, pageSize, sortBy);
            var items = new List<FeedbackResponseDto>();

            foreach (var feedback in feedbacks)
            {
                items.Add(await MapToResponseDtoAsync(feedback, currentUserId));
            }

            return new PaginatedResponse<FeedbackResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PaginatedResponse<FeedbackResponseDto>> GetFeedbackByUserId(int userId, int pageNumber, int pageSize)
        {
            // Verify user exists
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
            {
                throw new DomainExceptions($"Không tìm thấy người dùng với ID {userId}");
            }

            var (feedbacks, totalCount) = await _feedbackRepository.GetByUserId(userId, pageNumber, pageSize);
            var items = new List<FeedbackResponseDto>();
            
            foreach (var feedback in feedbacks)
            {
                items.Add(await MapToResponseDtoAsync(feedback));
            }

            return new PaginatedResponse<FeedbackResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<FeedbackResponseDto> UpdateFeedback(int feedbackId, UpdateFeedbackDto updateFeedbackDto, int userId)
        {
            var feedback = await _feedbackRepository.GetById(feedbackId);
            if (feedback == null)
            {
                throw new DomainExceptions($"Không tìm thấy đánh giá với ID {feedbackId}");
            }

            // Verify user owns this feedback
            if (feedback.UserId != userId)
            {
                throw new DomainExceptions("Người dùng không sở hữu đánh giá này");
            }

            // Feedback can only be updated once by the owner
            if (feedback.UpdatedAt.HasValue)
            {
                throw new DomainExceptions("Mỗi đánh giá chỉ được chỉnh sửa 1 lần");
            }

            // Validate and update DishId if provided
            if (updateFeedbackDto.DishId.HasValue)
            {
                var dish = await _dishRepository.GetByIdAsync(updateFeedbackDto.DishId.Value);
                if (dish == null)
                {
                    throw new DomainExceptions($"Không tìm thấy món ăn với ID {updateFeedbackDto.DishId.Value}");
                }

                // Validate dish belongs to the feedback's branch
                var branchDish = await _dishRepository.GetBranchDishAsync(feedback.BranchId, updateFeedbackDto.DishId.Value);
                if (branchDish == null)
                {
                    throw new DomainExceptions($"Món ăn với ID {updateFeedbackDto.DishId.Value} không thuộc chi nhánh của đánh giá này");
                }

                feedback.DishId = updateFeedbackDto.DishId;
            }

            // Validate rating if being updated
            if (updateFeedbackDto.Rating < 1 || updateFeedbackDto.Rating > 5)
            {
                throw new DomainExceptions("Điểm đánh giá phải nằm trong khoảng từ 1 đến 5");
            }

            // Capture old rating for metrics
            int oldRating = feedback.Rating;

            // Update feedback basic info
            feedback.Rating = updateFeedbackDto.Rating;
            feedback.Comment = updateFeedbackDto.Comment;
            feedback.UpdatedAt = DateTime.UtcNow;
            await _feedbackRepository.Update(feedback);

            // Recalculate metrics if rating changed
            if (oldRating != updateFeedbackDto.Rating)
            {
                await _branchMetricsService.OnFeedbackUpdated(feedback.BranchId, oldRating, updateFeedbackDto.Rating);
            }

            // Handle image updates (if ImageUrls is provided)
            if (updateFeedbackDto.ImageUrls != null)
            {
                // Remove all existing images
                var existingImages = await _feedbackRepository.GetImagesByFeedbackId(feedbackId);
                foreach (var img in existingImages)
                {
                    await _feedbackRepository.DeleteImage(img.FeedbackImageId);
                }

                // Add new images
                foreach (var imageUrl in updateFeedbackDto.ImageUrls)
                {
                    await _feedbackRepository.AddImage(new FeedbackImage
                    {
                        FeedbackId = feedbackId,
                        ImageUrl = imageUrl
                    });
                }
            }

            // Handle tag updates (if TagIds is provided)
            if (updateFeedbackDto.TagIds != null)
            {
                // Validate all tags exist
                foreach (var tagId in updateFeedbackDto.TagIds)
                {
                    var tagExists = await _feedbackTagRepository.Exists(tagId);
                    if (!tagExists)
                    {
                        throw new DomainExceptions($"Không tìm thấy tag với ID {tagId}");
                    }
                }

                // Remove all existing tags
                await _feedbackRepository.RemoveAllTags(feedbackId);

                // Add new tags
                foreach (var tagId in updateFeedbackDto.TagIds)
                {
                    await _feedbackRepository.AddTag(feedbackId, tagId);
                }
            }

            // Get updated feedback with all relations
            var updatedFeedback = await _feedbackRepository.GetById(feedbackId);
            return await MapToResponseDtoAsync(updatedFeedback);
        }

        public async Task<bool> DeleteFeedback(int feedbackId, int userId)
        {
            var feedback = await _feedbackRepository.GetById(feedbackId);
            if (feedback == null)
            {
                throw new DomainExceptions($"Không tìm thấy đánh giá với ID {feedbackId}");
            }

            // Verify user owns this feedback
            if (feedback.UserId != userId)
            {
                throw new DomainExceptions("Người dùng không sở hữu đánh giá này");
            }

            int rating = feedback.Rating;
            int branchId = feedback.BranchId;

            var result = await _feedbackRepository.Delete(feedbackId);

            if (result)
            {
                await _branchMetricsService.OnFeedbackDeleted(branchId, rating);
            }

            return result;
        }

        public async Task<double> GetAverageRatingByBranch(int branchId)
        {
            // Verify branch exists
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
            {
                throw new DomainExceptions($"Không tìm thấy chi nhánh với ID {branchId}");
            }

            return await _feedbackRepository.GetAverageRatingByBranchId(branchId);
        }

        public async Task<Dictionary<string, object>> GetFeedbackCountByBranch(int branchId)
        {
            // Verify branch exists
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
            {
                throw new DomainExceptions($"Không tìm thấy chi nhánh với ID {branchId}");
            }

            var count = await _feedbackRepository.GetCountByBranchId(branchId);
            var starCounts = await _feedbackRepository.GetFeedbackCountByStarsAsync(branchId);

            var result = new Dictionary<string, object>
            {
                { "branchId", branchId },
                { "feedbackCount", count },
                { "details", new Dictionary<string, int>
                    {
                        { "5", starCounts.GetValueOrDefault(5, 0) },
                        { "4", starCounts.GetValueOrDefault(4, 0) },
                        { "3", starCounts.GetValueOrDefault(3, 0) },
                        { "2", starCounts.GetValueOrDefault(2, 0) },
                        { "1", starCounts.GetValueOrDefault(1, 0) }
                    }
                }
            };

            return result;
        }

        public async Task<PaginatedResponse<FeedbackResponseDto>> GetFeedbackByRatingRange(
            int branchId, int minRating, int maxRating, int pageNumber, int pageSize)
        {
            // Verify branch exists
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
            {
                throw new DomainExceptions($"Không tìm thấy chi nhánh với ID {branchId}");
            }

            // Validate rating range
            if (minRating < 1 || maxRating > 5 || minRating > maxRating)
            {
                throw new DomainExceptions("Khoảng điểm đánh giá phải từ 1 đến 5, và min phải nhỏ hơn hoặc bằng max");
            }

            var (feedbacks, totalCount) = await _feedbackRepository.GetByRatingRange(branchId, minRating, maxRating, pageNumber, pageSize);
            var items = new List<FeedbackResponseDto>();
            
            foreach (var feedback in feedbacks)
            {
                items.Add(await MapToResponseDtoAsync(feedback));
            }

            return new PaginatedResponse<FeedbackResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<List<FeedbackImageDto>> GetFeedbackImages(int feedbackId)
        {
            var feedback = await _feedbackRepository.GetById(feedbackId);
            if (feedback == null)
            {
                throw new DomainExceptions($"Không tìm thấy đánh giá với ID {feedbackId}");
            }

            var images = await _feedbackRepository.GetImagesByFeedbackId(feedbackId);
            return images.Select(img => new FeedbackImageDto
            {
                Id = img.FeedbackImageId,
                Url = img.ImageUrl
            }).ToList();
        }

        // Helper method to map Feedback entity to ResponseDto
        private async Task<FeedbackResponseDto> MapToResponseDtoAsync(Feedback feedback, int? currentUserId = null)
        {
            var images = feedback.FeedbackImages?.Select(img => new FeedbackImageDto
            {
                Id = img.FeedbackImageId,
                Url = img.ImageUrl
            }).ToList() ?? new List<FeedbackImageDto>();

            var tags = feedback.FeedbackTagAssociations?.Select(fta => new FeedbackTagDto
            {
                Id = fta.FeedbackTag.TagId,
                Name = fta.FeedbackTag.TagName
            }).ToList() ?? new List<FeedbackTagDto>();

            FeedbackUserDto? userDto = null;
            if (feedback.User != null)
            {
                var selectedBadge = feedback.User.UserBadges?
                    .FirstOrDefault(ub => ub.IsSelected)?.Badge;

                userDto = new FeedbackUserDto
                {
                    Id = feedback.User.Id,
                    Name = $"{feedback.User.FirstName} {feedback.User.LastName}".Trim(),
                    Avatar = feedback.User.AvatarUrl,
                    SelectedBadgeName = selectedBadge?.BadgeName,
                    SelectedBadgeIconUrl = selectedBadge?.IconUrl
                };
            }

            FeedbackDishDto? dishDto = null;
            if (feedback.DishId.HasValue && feedback.Dish != null)
            {
                dishDto = new FeedbackDishDto
                {
                    Id = feedback.Dish.DishId,
                    Name = feedback.Dish.Name,
                    Price = feedback.Dish.Price,
                    ImageUrl = feedback.Dish.ImageUrl
                };
            }

            // Vote counts
            int upVotes = feedback.Votes?.Count(v => v.VoteType == VoteType.Up) ?? 0;
            int downVotes = feedback.Votes?.Count(v => v.VoteType == VoteType.Down) ?? 0;

            string? userVote = null;
            if (currentUserId.HasValue && feedback.Votes != null)
            {
                var vote = feedback.Votes.FirstOrDefault(v => v.UserId == currentUserId.Value);
                userVote = vote?.VoteType == VoteType.Up ? "up" :
                           vote?.VoteType == VoteType.Down ? "down" : null;
            }

            // Vendor reply
            VendorReplyDto? vendorReplyDto = null;
            if (feedback.VendorReply != null)
            {
                var replyUser = feedback.VendorReply.User;
                vendorReplyDto = new VendorReplyDto
                {
                    VendorReplyId = feedback.VendorReply.VendorReplyId,
                    Content = feedback.VendorReply.Content,
                    RepliedBy = replyUser != null ? $"{replyUser.FirstName} {replyUser.LastName}".Trim() : "Vendor",
                    CreatedAt = feedback.VendorReply.CreatedAt,
                    UpdatedAt = feedback.VendorReply.UpdatedAt
                };
            }

            return new FeedbackResponseDto
            {
                Id = feedback.FeedbackId,
                User = userDto,
                OrderId = feedback.OrderId,
                DishId = feedback.DishId,
                Dish = dishDto,
                BranchId = feedback.BranchId,
                Rating = feedback.Rating,
                  Comment = feedback.Comment,
                  FeedbackXP = feedback.FeedbackXP,
                  CreatedAt = feedback.CreatedAt,
                UpdatedAt = feedback.UpdatedAt,
                Images = images,
                Tags = tags,
                UpVotes = upVotes,
                DownVotes = downVotes,
                NetScore = upVotes - downVotes,
                UserVote = userVote,
                VendorReply = vendorReplyDto
            };
        }

        public async Task<VelocityCheckDto> CheckVelocityAsync(int userId, int? branchId = null, double? userLat = null, double? userLong = null)
        {
            var today = DateTime.UtcNow.Date;
            var dailyLimit = _settingService.GetInt(FeedbackDailyLimitSettingName, 3);

            var todayCount = await _feedbackRepository.GetDailyFeedbackCountWithoutOrderAsync(userId, today);
            var reviewedBranchIds = await _feedbackRepository.GetReviewedBranchIdsTodayWithoutOrderAsync(userId, today);

            double? distanceMeters = null;
            double? maxDistanceMeters = null;
            bool? isWithinDistance = null;
            bool? canReviewWithoutOrder = null;

            var hasBranchInput = branchId.HasValue || userLat.HasValue || userLong.HasValue;
            if (hasBranchInput)
            {
                if (!branchId.HasValue || !userLat.HasValue || !userLong.HasValue)
                {
                    throw new DomainExceptions("Vui lòng cung cấp đầy đủ branchId, userLat và userLong để kiểm tra khoảng cách");
                }

                var branch = await _branchRepository.GetByIdAsync(branchId.Value);
                if (branch == null)
                {
                    throw new DomainExceptions($"Không tìm thấy chi nhánh với ID {branchId.Value}");
                }

                var distanceKm = HaversineDistance(userLat.Value, userLong.Value, branch.Lat, branch.Long);
                distanceMeters = Math.Round(distanceKm * 1000, 2);
                maxDistanceMeters = Math.Round(MaxDistanceWithoutOrderKm * 1000, 2);
                isWithinDistance = distanceKm <= MaxDistanceWithoutOrderKm;

                var hasNonOrderFeedbackForBranch = await _feedbackRepository.HasUserFeedbackOnBranchWithoutOrderAsync(branchId.Value, userId);
                canReviewWithoutOrder = isWithinDistance.Value && todayCount < dailyLimit && !hasNonOrderFeedbackForBranch;
            }

            return new VelocityCheckDto
            {
                RemainingTotalToday = Math.Max(0, dailyLimit - todayCount),
                DailyLimit = dailyLimit,
                ReviewedBranchIds = reviewedBranchIds,
                BranchId = branchId,
                DistanceMeters = distanceMeters,
                MaxDistanceMeters = maxDistanceMeters,
                IsWithinDistance = isWithinDistance,
                CanReviewWithoutOrder = canReviewWithoutOrder
            };
        }

        private static double HaversineDistance(double lat1, double long1, double lat2, double long2)
        {
            const double earthRadiusKm = 6371;

            double dLat = DegreesToRadians(lat2 - lat1);
            double dLong = DegreesToRadians(long2 - long1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                       Math.Sin(dLong / 2) * Math.Sin(dLong / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
    }
}
