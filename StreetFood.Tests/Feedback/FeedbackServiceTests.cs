using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BO.DTO.Feedback;
using BO.Entities;
using BO.Enums;
using BO.Exceptions;
using BO.Common;
using Moq;
using Repository.Interfaces;
using Service;
using Service.Interfaces;
using Xunit;

namespace StreetFood.Tests.FeedbackTests
{
    public class FeedbackServiceTests
    {
        private readonly Mock<IFeedbackRepository> _feedbackRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IBranchRepository> _branchRepoMock;
        private readonly Mock<IFeedbackTagRepository> _tagRepoMock;
        private readonly Mock<IDishRepository> _dishRepoMock;
        private readonly Mock<IBranchMetricsService> _metricsServiceMock;
        private readonly Mock<INotificationService> _notifServiceMock;
        private readonly Mock<IOrderRepository> _orderRepoMock;
        private readonly Mock<IQuestProgressService> _questServiceMock;
        private readonly Mock<ISettingService> _settingServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly FeedbackService _feedbackService;

        public FeedbackServiceTests()
        {
            _feedbackRepoMock = new Mock<IFeedbackRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _branchRepoMock = new Mock<IBranchRepository>();
            _tagRepoMock = new Mock<IFeedbackTagRepository>();
            _dishRepoMock = new Mock<IDishRepository>();
            _metricsServiceMock = new Mock<IBranchMetricsService>();
            _notifServiceMock = new Mock<INotificationService>();
            _orderRepoMock = new Mock<IOrderRepository>();
            _questServiceMock = new Mock<IQuestProgressService>();
            _settingServiceMock = new Mock<ISettingService>();
            _userServiceMock = new Mock<IUserService>();

            _feedbackService = new FeedbackService(
                _feedbackRepoMock.Object,
                _userRepoMock.Object,
                _branchRepoMock.Object,
                _tagRepoMock.Object,
                _dishRepoMock.Object,
                _metricsServiceMock.Object,
                _notifServiceMock.Object,
                _orderRepoMock.Object,
                _questServiceMock.Object,
                _settingServiceMock.Object,
                _userServiceMock.Object
            );
        }

        // --- SECTION: CREATE FEEDBACK (SV_FEEDBACK_01) ---

        // SV_FEEDBACK_01 (UTCID01) - Success with Order
        [Fact]
        public async Task CreateFeedback_WithOrder_Success()
        {
            var userId = 1; var branchId = 5; var orderId = 100;
            var request = new CreateFeedbackDto { BranchId = branchId, OrderId = orderId, Rating = 5, Comment = "Good" };
            
            _userRepoMock.Setup(r => r.GetUserById(userId)).ReturnsAsync(new User { Id = userId });
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(new BO.Entities.Branch { BranchId = branchId, Name = "Test Branch" });
            _orderRepoMock.Setup(r => r.GetById(orderId)).ReturnsAsync(new BO.Entities.Order { OrderId = orderId, UserId = userId, BranchId = branchId, Status = OrderStatus.Complete });
            _feedbackRepoMock.Setup(r => r.HasFeedbackForOrder(userId, orderId)).ReturnsAsync(false);
            _feedbackRepoMock.Setup(r => r.Create(It.IsAny<Feedback>(), null, It.IsAny<List<int>>())).ReturnsAsync(new Feedback { FeedbackId = 1, Rating = 5 });

            var result = await _feedbackService.CreateFeedback(request, userId);

            Assert.Equal(5, result.Rating);
            _metricsServiceMock.Verify(s => s.OnFeedbackCreated(branchId, 5), Times.Once);
            _questServiceMock.Verify(s => s.UpdateProgressAsync(userId, QuestTaskType.REVIEW, 1), Times.Once);
        }

        // SV_FEEDBACK_01 (UTCID02) - Success without Order (GPS Within 300m)
        [Fact]
        public async Task CreateFeedback_WithoutOrder_WithinDistance_Success()
        {
            var userId = 1; var branchId = 5;
            var request = new CreateFeedbackDto { BranchId = branchId, Rating = 4, Comment = "Near", UserLat = 10.7, UserLong = 106.6 };
            
            _userRepoMock.Setup(r => r.GetUserById(userId)).ReturnsAsync(new User { Id = userId });
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(new BO.Entities.Branch { BranchId = branchId, Lat = 10.7, Long = 106.6 }); // Same spot
            _settingServiceMock.Setup(s => s.GetInt("feedbackDailyLimit", 3)).Returns(3);
            _feedbackRepoMock.Setup(r => r.HasUserFeedbackOnBranchWithoutOrderAsync(branchId, userId)).ReturnsAsync(false);
            _feedbackRepoMock.Setup(r => r.GetReviewedBranchIdsTodayWithoutOrderAsync(userId, It.IsAny<DateTime>())).ReturnsAsync(new List<int>());
            _feedbackRepoMock.Setup(r => r.Create(It.IsAny<Feedback>(), null, It.IsAny<List<int>>())).ReturnsAsync(new Feedback { FeedbackId = 1, Rating = 4 });

            var result = await _feedbackService.CreateFeedback(request, userId);

            Assert.Equal(4, result.Rating);
        }

        // SV_FEEDBACK_01 (UTCID03) - Order Not Complete
        [Fact]
        public async Task CreateFeedback_OrderNotComplete_ThrowsException()
        {
            var userId = 1; var branchId = 5; var orderId = 100;
            var request = new CreateFeedbackDto { BranchId = branchId, OrderId = orderId, Rating = 5 };
            
            _userRepoMock.Setup(r => r.GetUserById(userId)).ReturnsAsync(new User { Id = userId });
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(new BO.Entities.Branch { BranchId = branchId });
            _orderRepoMock.Setup(r => r.GetById(orderId)).ReturnsAsync(new BO.Entities.Order { OrderId = orderId, UserId = userId, BranchId = branchId, Status = OrderStatus.Pending });

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _feedbackService.CreateFeedback(request, userId));
            Assert.Contains("phải hoàn thành", ex.Message);
        }

        // SV_FEEDBACK_01 (UTCID04) - Already Reviewed Order
        [Fact]
        public async Task CreateFeedback_AlreadyReviewed_ThrowsException()
        {
            var userId = 1; var branchId = 5; var orderId = 100;
            var request = new CreateFeedbackDto { BranchId = branchId, OrderId = orderId, Rating = 5 };
            
            _userRepoMock.Setup(r => r.GetUserById(userId)).ReturnsAsync(new User { Id = userId });
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(new BO.Entities.Branch { BranchId = branchId });
            _orderRepoMock.Setup(r => r.GetById(orderId)).ReturnsAsync(new BO.Entities.Order { OrderId = orderId, UserId = userId, BranchId = branchId, Status = OrderStatus.Complete });
            _feedbackRepoMock.Setup(r => r.HasFeedbackForOrder(userId, orderId)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _feedbackService.CreateFeedback(request, userId));
            Assert.Contains("đã đánh giá", ex.Message);
        }

        // SV_FEEDBACK_01 (UTCID05) - Too Far (GPS > 300m)
        [Fact]
        public async Task CreateFeedback_TooFar_ThrowsException()
        {
            var userId = 1; var branchId = 5;
            var request = new CreateFeedbackDto { BranchId = branchId, UserLat = 10.0, UserLong = 100.0, Rating = 5 }; // Very far from 10.7/106.6
            
            _userRepoMock.Setup(r => r.GetUserById(userId)).ReturnsAsync(new User { Id = userId });
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(new BO.Entities.Branch { BranchId = branchId, Lat = 10.7, Long = 106.6 });

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _feedbackService.CreateFeedback(request, userId));
            Assert.Contains("phạm vi 300m", ex.Message);
        }

        // SV_FEEDBACK_01 (UTCID06) - Daily Limit Reached
        [Fact]
        public async Task CreateFeedback_DailyLimit_ThrowsException()
        {
            var userId = 1; var branchId = 5;
            var request = new CreateFeedbackDto { BranchId = branchId, UserLat = 10.7, UserLong = 106.6, Rating = 5 };
            
            _userRepoMock.Setup(r => r.GetUserById(userId)).ReturnsAsync(new User { Id = userId });
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(new BO.Entities.Branch { BranchId = branchId, Lat = 10.7, Long = 106.6 });
            _settingServiceMock.Setup(s => s.GetInt("feedbackDailyLimit", 3)).Returns(1); // Limit 1
            _feedbackRepoMock.Setup(r => r.GetReviewedBranchIdsTodayWithoutOrderAsync(userId, It.IsAny<DateTime>())).ReturnsAsync(new List<int> { 99 }); // Alreay reviewed branch 99

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _feedbackService.CreateFeedback(request, userId));
            Assert.Contains("đạt giới hạn", ex.Message);
        }

        // SV_FEEDBACK_01 (UTCID07) - Dish Not in Branch
        [Fact]
        public async Task CreateFeedback_DishNotInBranch_ThrowsException()
        {
            var userId = 1; var branchId = 5; var dishId = 20;
            var request = new CreateFeedbackDto { BranchId = branchId, DishId = dishId, Rating = 5 };
            
            _userRepoMock.Setup(r => r.GetUserById(userId)).ReturnsAsync(new User { Id = userId });
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(new BO.Entities.Branch { BranchId = branchId });
            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(new BO.Entities.Dish { DishId = dishId });
            _dishRepoMock.Setup(r => r.GetBranchDishAsync(branchId, dishId)).ReturnsAsync((BranchDish?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _feedbackService.CreateFeedback(request, userId));
            Assert.Contains("không thuộc chi nhánh", ex.Message);
        }

        // --- SECTION: UPDATE FEEDBACK (SV_FEEDBACK_02) ---

        // SV_FEEDBACK_02 (UTCID01) - Success
        [Fact]
        public async Task UpdateFeedback_Success()
        {
            var userId = 1; var feedbackId = 50;
            var request = new UpdateFeedbackDto { Rating = 4, Comment = "Updated Comment" };
            var feedback = new Feedback { FeedbackId = feedbackId, UserId = userId, BranchId = 5, Rating = 2 };

            _feedbackRepoMock.Setup(r => r.GetById(feedbackId)).ReturnsAsync(feedback);
            _feedbackRepoMock.Setup(r => r.Update(It.IsAny<Feedback>())).ReturnsAsync(feedback);

            var result = await _feedbackService.UpdateFeedback(feedbackId, request, userId);

            Assert.Equal(4, result.Rating);
            Assert.Equal("Updated Comment", result.Comment);
            _metricsServiceMock.Verify(s => s.OnFeedbackUpdated(5, 2, 4), Times.Once);
        }

        // SV_FEEDBACK_02 (UTCID02) - Feedback Not Found
        [Fact]
        public async Task UpdateFeedback_NotFound_ThrowsException()
        {
            _feedbackRepoMock.Setup(r => r.GetById(It.IsAny<int>())).ReturnsAsync((Feedback?)null);
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _feedbackService.UpdateFeedback(999, new UpdateFeedbackDto { Rating = 5 }, 1));
            Assert.Contains("Không tìm thấy", ex.Message);
        }

        // SV_FEEDBACK_02 (UTCID03) - Unauthorized (Not Owner)
        [Fact]
        public async Task UpdateFeedback_Unauthorized_ThrowsException()
        {
            var feedback = new Feedback { FeedbackId = 50, UserId = 999 };
            _feedbackRepoMock.Setup(r => r.GetById(50)).ReturnsAsync(feedback);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _feedbackService.UpdateFeedback(50, new UpdateFeedbackDto(), 1));
            Assert.Contains("không sở hữu", ex.Message);
        }

        // SV_FEEDBACK_02 (UTCID04) - Already Updated
        [Fact]
        public async Task UpdateFeedback_AlreadyUpdated_ThrowsException()
        {
            var feedback = new Feedback { FeedbackId = 50, UserId = 1, UpdatedAt = DateTime.UtcNow };
            _feedbackRepoMock.Setup(r => r.GetById(50)).ReturnsAsync(feedback);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _feedbackService.UpdateFeedback(50, new UpdateFeedbackDto(), 1));
            Assert.Contains("chỉnh sửa 1 lần", ex.Message);
        }

        // SV_FEEDBACK_02 (UTCID05) - Dish in Diff Branch
        [Fact]
        public async Task UpdateFeedback_DishInDiffBranch_ThrowsException()
        {
            var userId = 1; var feedbackId = 50; var dishId = 20;
            var feedback = new Feedback { FeedbackId = feedbackId, UserId = userId, BranchId = 5 };
            var request = new UpdateFeedbackDto { DishId = dishId, Rating = 5 };

            _feedbackRepoMock.Setup(r => r.GetById(feedbackId)).ReturnsAsync(feedback);
            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(new BO.Entities.Dish { DishId = dishId });
            _dishRepoMock.Setup(r => r.GetBranchDishAsync(5, dishId)).ReturnsAsync((BranchDish?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _feedbackService.UpdateFeedback(feedbackId, request, userId));
            Assert.Contains("không thuộc chi nhánh", ex.Message);
        }

        // SV_FEEDBACK_02 (UTCID06) - Rating Out of Range
        [Fact]
        public async Task UpdateFeedback_RatingOutOfRange_ThrowsException()
        {
            var userId = 1; var feedbackId = 50;
            var feedback = new Feedback { FeedbackId = feedbackId, UserId = userId };
            var request = new UpdateFeedbackDto { Rating = 6 };

            _feedbackRepoMock.Setup(r => r.GetById(feedbackId)).ReturnsAsync(feedback);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _feedbackService.UpdateFeedback(feedbackId, request, userId));
            Assert.Contains("từ 1 đến 5", ex.Message);
        }

        // SV_FEEDBACK_02 (UTCID07) - Tag Not Found
        [Fact]
        public async Task UpdateFeedback_TagNotFound_ThrowsException()
        {
            var userId = 1; var feedbackId = 50;
            var feedback = new Feedback { FeedbackId = feedbackId, UserId = userId };
            var request = new UpdateFeedbackDto { TagIds = new List<int> { 99 }, Rating = 5 };

            _feedbackRepoMock.Setup(r => r.GetById(feedbackId)).ReturnsAsync(feedback);
            _tagRepoMock.Setup(r => r.Exists(99)).ReturnsAsync(false);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _feedbackService.UpdateFeedback(feedbackId, request, userId));
            Assert.Contains("Không tìm thấy tag", ex.Message);
        }
    }
}