using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BO.DTO.Feedback;
using BO.Entities;
using BO.Enums;
using BO.Exceptions;
using Moq;
using Repository.Interfaces;
using Service;
using Service.Interfaces;
using FeedbackEntity = BO.Entities.Feedback;

namespace StreetFood.Tests.FeedbackTests;

public class FeedbackServiceTests
{
    private readonly Mock<IFeedbackRepository> _feedbackRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IBranchRepository> _branchRepository = new();
    private readonly Mock<IFeedbackTagRepository> _feedbackTagRepository = new();
    private readonly Mock<IDishRepository> _dishRepository = new();
    private readonly Mock<IBranchMetricsService> _branchMetricsService = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IQuestProgressService> _questProgressService = new();
    private readonly Mock<ISettingService> _settingService = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly FeedbackService _service;

    public FeedbackServiceTests()
    {
        _settingService.Setup(s => s.GetInt("feedbackXP", 0)).Returns(0);
        _settingService.Setup(s => s.GetInt("feedbackDailyLimit", 3)).Returns(3);
        _branchMetricsService.Setup(s => s.OnFeedbackCreated(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);
        _branchMetricsService.Setup(s => s.OnFeedbackUpdated(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);
        _branchMetricsService.Setup(s => s.OnFeedbackDeleted(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);
        _branchMetricsService.Setup(s => s.RecalculateFromScratch(It.IsAny<int>())).Returns(Task.CompletedTask);
        _questProgressService.Setup(s => s.UpdateProgressAsync(It.IsAny<int>(), It.IsAny<QuestTaskType>(), It.IsAny<int>())).Returns(Task.CompletedTask);
        _questProgressService.Setup(s => s.HandleTierUpAsync(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);
        _notificationService.Setup(s => s.NotifyAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<object?>())).Returns(Task.CompletedTask);
        _userService.Setup(s => s.AddXPAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);

        _service = new FeedbackService(
            _feedbackRepository.Object,
            _userRepository.Object,
            _branchRepository.Object,
            _feedbackTagRepository.Object,
            _dishRepository.Object,
            _branchMetricsService.Object,
            _notificationService.Object,
            _orderRepository.Object,
            _questProgressService.Object,
            _settingService.Object,
            _userService.Object);
    }

    private static User MakeUser(int id = 1)
    {
        return new User
        {
            Id = id,
            UserName = "tester",
            FirstName = "Test",
            LastName = "User",
            Email = "tester@example.com"
        };
    }

    private static Branch MakeBranch(int id = 10, double lat = 10.0000, double lng = 106.0000, int? managerId = null)
    {
        return new Branch
        {
            BranchId = id,
            Name = "Sample Branch",
            AddressDetail = "123 Sample Street",
            City = "Ho Chi Minh City",
            Ward = "Ward 1",
            Lat = lat,
            Long = lng,
            ManagerId = managerId,
            IsActive = true,
            IsSubscribed = false,
            AvgRating = 0,
            TierId = 2
        };
    }

    private static Order MakeOrder(int id, int userId, int branchId, OrderStatus status)
    {
        return new Order
        {
            OrderId = id,
            UserId = userId,
            BranchId = branchId,
            Status = status,
            TotalAmount = 100000m,
            FinalAmount = 100000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static FeedbackEntity MakeCreatedFeedback(int feedbackId, int userId, int branchId, int? orderId = null)
    {
        return new FeedbackEntity
        {
            FeedbackId = feedbackId,
            UserId = userId,
            BranchId = branchId,
            OrderId = orderId,
            Rating = 5,
            Comment = "Great",
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task CreateFeedback_WithCompletedOrder_AllowsFeedbackWithoutGps()
    {
        var userId = 1;
        var branchId = 10;
        var orderId = 99;
        var user = MakeUser(userId);
        var branch = MakeBranch(branchId);
        var order = MakeOrder(orderId, userId, branchId, OrderStatus.Complete);
        var createdFeedback = MakeCreatedFeedback(100, userId, branchId, orderId);

        _userRepository.Setup(r => r.GetUserById(userId)).ReturnsAsync(user);
        _branchRepository.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
        _orderRepository.Setup(r => r.GetById(orderId)).ReturnsAsync(order);
        _feedbackRepository.Setup(r => r.HasFeedbackForOrder(userId, orderId)).ReturnsAsync(false);
        _feedbackRepository.Setup(r => r.Create(It.IsAny<FeedbackEntity>(), It.IsAny<List<string>>(), It.IsAny<List<int>>()))
            .ReturnsAsync(createdFeedback);

        var result = await _service.CreateFeedback(new CreateFeedbackDto
        {
            BranchId = branchId,
            OrderId = orderId,
            Rating = 5,
            Comment = "Nice food"
        }, userId);

        Assert.Equal(100, result.Id);
        Assert.Equal(branchId, result.BranchId);
        Assert.Equal(5, result.Rating);
        _feedbackRepository.Verify(r => r.Create(It.IsAny<FeedbackEntity>(), It.IsAny<List<string>>(), It.IsAny<List<int>>()), Times.Once);
    }

    [Fact]
    public async Task CreateFeedback_WithPendingOrder_Throws()
    {
        var userId = 1;
        var branchId = 10;
        var orderId = 99;

        _userRepository.Setup(r => r.GetUserById(userId)).ReturnsAsync(MakeUser(userId));
        _branchRepository.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(MakeBranch(branchId));
        _orderRepository.Setup(r => r.GetById(orderId)).ReturnsAsync(MakeOrder(orderId, userId, branchId, OrderStatus.Paid));

        var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _service.CreateFeedback(new CreateFeedbackDto
        {
            BranchId = branchId,
            OrderId = orderId,
            Rating = 5,
            Comment = "Nice food"
        }, userId));

        Assert.Contains("hoàn thành", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateFeedback_WithoutOrder_WithinGpsRange_AllowsFeedback()
    {
        var userId = 1;
        var branchId = 10;
        var user = MakeUser(userId);
        var branch = MakeBranch(branchId, 10.0000, 106.0000);
        var createdFeedback = MakeCreatedFeedback(100, userId, branchId);

        _userRepository.Setup(r => r.GetUserById(userId)).ReturnsAsync(user);
        _branchRepository.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
        _feedbackRepository.Setup(r => r.HasUserFeedbackOnBranchWithoutOrderAsync(branchId, userId)).ReturnsAsync(false);
        _feedbackRepository.Setup(r => r.GetReviewedBranchIdsTodayWithoutOrderAsync(userId, It.IsAny<DateTime>())).ReturnsAsync(new List<int> { 11, 12 });
        _feedbackRepository.Setup(r => r.Create(It.IsAny<FeedbackEntity>(), It.IsAny<List<string>>(), It.IsAny<List<int>>()))
            .ReturnsAsync(createdFeedback);

        var result = await _service.CreateFeedback(new CreateFeedbackDto
        {
            BranchId = branchId,
            Rating = 5,
            Comment = "Nice food",
            UserLat = 10.0010,
            UserLong = 106.0000
        }, userId);

        Assert.Equal(100, result.Id);
        Assert.Equal(branchId, result.BranchId);
        Assert.Equal(5, result.Rating);
        _feedbackRepository.Verify(r => r.Create(It.IsAny<FeedbackEntity>(), It.IsAny<List<string>>(), It.IsAny<List<int>>()), Times.Once);
    }

    [Fact]
    public async Task CreateFeedback_WithoutOrder_MissingGps_Throws()
    {
        var userId = 1;
        var branchId = 10;

        _userRepository.Setup(r => r.GetUserById(userId)).ReturnsAsync(MakeUser(userId));
        _branchRepository.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(MakeBranch(branchId));

        var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _service.CreateFeedback(new CreateFeedbackDto
        {
            BranchId = branchId,
            Rating = 5,
            Comment = "Nice food"
        }, userId));

        Assert.Contains("GPS", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateFeedback_WithoutOrder_TooFar_Throws()
    {
        var userId = 1;
        var branchId = 10;

        _userRepository.Setup(r => r.GetUserById(userId)).ReturnsAsync(MakeUser(userId));
        _branchRepository.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(MakeBranch(branchId, 10.0000, 106.0000));

        var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _service.CreateFeedback(new CreateFeedbackDto
        {
            BranchId = branchId,
            Rating = 5,
            Comment = "Nice food",
            UserLat = 10.0050,
            UserLong = 106.0000
        }, userId));

        Assert.Contains("300m", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateFeedback_WithoutOrder_SameBranchTwice_Throws()
    {
        var userId = 1;
        var branchId = 10;

        _userRepository.Setup(r => r.GetUserById(userId)).ReturnsAsync(MakeUser(userId));
        _branchRepository.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(MakeBranch(branchId));
        _feedbackRepository.Setup(r => r.HasUserFeedbackOnBranchWithoutOrderAsync(branchId, userId)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _service.CreateFeedback(new CreateFeedbackDto
        {
            BranchId = branchId,
            Rating = 5,
            Comment = "Nice food",
            UserLat = 10.0010,
            UserLong = 106.0000
        }, userId));

        Assert.Contains("1 lần", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateFeedback_WithoutOrder_MoreThanThreeBranchesPerDay_Throws()
    {
        var userId = 1;
        var branchId = 10;

        _userRepository.Setup(r => r.GetUserById(userId)).ReturnsAsync(MakeUser(userId));
        _branchRepository.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(MakeBranch(branchId));
        _settingService.Setup(s => s.GetInt("feedbackDailyLimit", 3)).Returns(2);
        _feedbackRepository.Setup(r => r.HasUserFeedbackOnBranchWithoutOrderAsync(branchId, userId)).ReturnsAsync(false);
        _feedbackRepository.Setup(r => r.GetReviewedBranchIdsTodayWithoutOrderAsync(userId, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<int> { 1, 2 });

        var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _service.CreateFeedback(new CreateFeedbackDto
        {
            BranchId = branchId,
            Rating = 5,
            Comment = "Nice food",
            UserLat = 10.0010,
            UserLong = 106.0000
        }, userId));

        Assert.Contains("2 quán", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckVelocityAsync_WithBranchAndGps_ReturnsDistanceAndConfiguredLimit()
    {
        var userId = 1;
        var branchId = 10;

        _settingService.Setup(s => s.GetInt("feedbackDailyLimit", 3)).Returns(5);

        _feedbackRepository.Setup(r => r.GetDailyFeedbackCountWithoutOrderAsync(userId, It.IsAny<DateTime>())).ReturnsAsync(2);
        _feedbackRepository.Setup(r => r.GetReviewedBranchIdsTodayWithoutOrderAsync(userId, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<int> { 10, 11 });
        _feedbackRepository.Setup(r => r.HasUserFeedbackOnBranchWithoutOrderAsync(branchId, userId)).ReturnsAsync(false);
        _branchRepository.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(MakeBranch(branchId, 10.0000, 106.0000));

        var result = await _service.CheckVelocityAsync(userId, branchId, 10.0010, 106.0000);

        Assert.Equal(3, result.RemainingTotalToday);
        Assert.Equal(5, result.DailyLimit);
        Assert.Equal(new List<int> { 10, 11 }, result.ReviewedBranchIds);
        Assert.Equal(branchId, result.BranchId);
        Assert.True(result.IsWithinDistance);
        Assert.True(result.CanReviewWithoutOrder);
        Assert.NotNull(result.DistanceMeters);
        Assert.True(result.DistanceMeters.Value > 0 && result.DistanceMeters.Value < 300);
    }
}