using BO.Entities;
using FluentAssertions;
using Moq;
using Repository.Interfaces;
using Service;

namespace Tests;

public class QuestProgressServiceTests
{
    private readonly Mock<IUserQuestRepository> _userQuestRepoMock;
    private readonly Mock<IUserBadgeRepository> _userBadgeRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IUserVoucherRepository> _userVoucherRepoMock;
    private readonly QuestProgressService _sut;

    public QuestProgressServiceTests()
    {
        _userQuestRepoMock = new Mock<IUserQuestRepository>();
        _userBadgeRepoMock = new Mock<IUserBadgeRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _userVoucherRepoMock = new Mock<IUserVoucherRepository>();

        _sut = new QuestProgressService(
            _userQuestRepoMock.Object,
            _userBadgeRepoMock.Object,
            _userRepoMock.Object,
            _userVoucherRepoMock.Object);
    }

    #region Helper Methods

    private static UserQuestTask MakeUserQuestTask(
        int userQuestTaskId = 1,
        int userQuestId = 10,
        int currentValue = 0,
        int targetValue = 5,
        string rewardType = "POINTS",
        int rewardValue = 100,
        string taskType = "REVIEW")
    {
        return new UserQuestTask
        {
            UserQuestTaskId = userQuestTaskId,
            UserQuestId = userQuestId,
            QuestTaskId = 1,
            CurrentValue = currentValue,
            IsCompleted = false,
            RewardClaimed = false,
            QuestTask = new QuestTask
            {
                QuestTaskId = 1,
                QuestId = 1,
                Type = taskType,
                TargetValue = targetValue,
                RewardType = rewardType,
                RewardValue = rewardValue
            }
        };
    }

    #endregion

    #region UpdateProgressAsync — Increment Logic

    [Fact]
    public async Task UpdateProgressAsync_IncrementsCurrentValue()
    {
        // Arrange
        var task = MakeUserQuestTask(currentValue: 2, targetValue: 5);
        _userQuestRepoMock
            .Setup(r => r.GetInProgressTasksByTypeAsync(100, "REVIEW"))
            .ReturnsAsync(new List<UserQuestTask> { task });
        _userQuestRepoMock
            .Setup(r => r.UpdateUserQuestTaskAsync(It.IsAny<UserQuestTask>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateProgressAsync(100, "REVIEW", 1);

        // Assert
        task.CurrentValue.Should().Be(3);
        task.IsCompleted.Should().BeFalse();
        _userQuestRepoMock.Verify(r => r.UpdateUserQuestTaskAsync(task), Times.Once);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenNoMatchingTasks_DoesNothing()
    {
        // Arrange
        _userQuestRepoMock
            .Setup(r => r.GetInProgressTasksByTypeAsync(100, "REVIEW"))
            .ReturnsAsync(new List<UserQuestTask>());

        // Act
        await _sut.UpdateProgressAsync(100, "REVIEW", 1);

        // Assert
        _userQuestRepoMock.Verify(
            r => r.UpdateUserQuestTaskAsync(It.IsAny<UserQuestTask>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProgressAsync_UpdatesMultipleMatchingTasks()
    {
        // Arrange
        var task1 = MakeUserQuestTask(userQuestTaskId: 1, userQuestId: 10, currentValue: 3, targetValue: 5);
        var task2 = MakeUserQuestTask(userQuestTaskId: 2, userQuestId: 20, currentValue: 0, targetValue: 3);
        _userQuestRepoMock
            .Setup(r => r.GetInProgressTasksByTypeAsync(100, "REVIEW"))
            .ReturnsAsync(new List<UserQuestTask> { task1, task2 });
        _userQuestRepoMock
            .Setup(r => r.UpdateUserQuestTaskAsync(It.IsAny<UserQuestTask>()))
            .Returns(Task.CompletedTask);
        _userQuestRepoMock
            .Setup(r => r.AreAllTasksCompletedAsync(It.IsAny<int>()))
            .ReturnsAsync(false);

        // Act
        await _sut.UpdateProgressAsync(100, "REVIEW", 1);

        // Assert
        task1.CurrentValue.Should().Be(4);
        task2.CurrentValue.Should().Be(1);
        _userQuestRepoMock.Verify(
            r => r.UpdateUserQuestTaskAsync(It.IsAny<UserQuestTask>()), Times.Exactly(2));
    }

    #endregion

    #region UpdateProgressAsync — Task Completion

    [Fact]
    public async Task UpdateProgressAsync_WhenTargetReached_MarksTaskCompleted()
    {
        // Arrange
        var task = MakeUserQuestTask(currentValue: 4, targetValue: 5);
        _userQuestRepoMock
            .Setup(r => r.GetInProgressTasksByTypeAsync(100, "REVIEW"))
            .ReturnsAsync(new List<UserQuestTask> { task });
        _userQuestRepoMock
            .Setup(r => r.UpdateUserQuestTaskAsync(It.IsAny<UserQuestTask>()))
            .Returns(Task.CompletedTask);
        _userQuestRepoMock.Setup(r => r.AreAllTasksCompletedAsync(10)).ReturnsAsync(false);

        // Act
        await _sut.UpdateProgressAsync(100, "REVIEW", 1);

        // Assert
        task.CurrentValue.Should().Be(5);
        task.IsCompleted.Should().BeTrue();
        task.CompletedAt.Should().NotBeNull();
        task.RewardClaimed.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenExceedsTarget_StillMarksCompleted()
    {
        // Arrange
        var task = MakeUserQuestTask(currentValue: 3, targetValue: 5);
        _userQuestRepoMock
            .Setup(r => r.GetInProgressTasksByTypeAsync(100, "REVIEW"))
            .ReturnsAsync(new List<UserQuestTask> { task });
        _userQuestRepoMock
            .Setup(r => r.UpdateUserQuestTaskAsync(It.IsAny<UserQuestTask>()))
            .Returns(Task.CompletedTask);
        _userQuestRepoMock.Setup(r => r.AreAllTasksCompletedAsync(10)).ReturnsAsync(false);

        // Act
        await _sut.UpdateProgressAsync(100, "REVIEW", 5); // 3 + 5 = 8 > 5

        // Assert
        task.CurrentValue.Should().Be(8);
        task.IsCompleted.Should().BeTrue();
    }

    #endregion

    #region UpdateProgressAsync — Reward Distribution (POINTS)

    [Fact]
    public async Task UpdateProgressAsync_PointsReward_AddsPointsToUser()
    {
        // Arrange
        var task = MakeUserQuestTask(
            currentValue: 4, targetValue: 5,
            rewardType: "POINTS", rewardValue: 100);
        var user = new User { Id = 100, Point = 500 };

        _userQuestRepoMock
            .Setup(r => r.GetInProgressTasksByTypeAsync(100, "REVIEW"))
            .ReturnsAsync(new List<UserQuestTask> { task });
        _userQuestRepoMock
            .Setup(r => r.UpdateUserQuestTaskAsync(It.IsAny<UserQuestTask>()))
            .Returns(Task.CompletedTask);
        _userQuestRepoMock.Setup(r => r.AreAllTasksCompletedAsync(10)).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.GetUserById(100)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateProgressAsync(100, "REVIEW", 1);

        // Assert
        user.Point.Should().Be(600); // 500 + 100
        _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    #endregion

    #region UpdateProgressAsync — Reward Distribution (BADGE)

    [Fact]
    public async Task UpdateProgressAsync_BadgeReward_CreatesUserBadge()
    {
        // Arrange
        var task = MakeUserQuestTask(
            currentValue: 4, targetValue: 5,
            rewardType: "BADGE", rewardValue: 42);

        _userQuestRepoMock
            .Setup(r => r.GetInProgressTasksByTypeAsync(100, "REVIEW"))
            .ReturnsAsync(new List<UserQuestTask> { task });
        _userQuestRepoMock
            .Setup(r => r.UpdateUserQuestTaskAsync(It.IsAny<UserQuestTask>()))
            .Returns(Task.CompletedTask);
        _userQuestRepoMock.Setup(r => r.AreAllTasksCompletedAsync(10)).ReturnsAsync(false);
        _userBadgeRepoMock
            .Setup(r => r.Create(It.IsAny<UserBadge>()))
            .ReturnsAsync((UserBadge ub) => ub);

        // Act
        await _sut.UpdateProgressAsync(100, "REVIEW", 1);

        // Assert
        _userBadgeRepoMock.Verify(r => r.Create(It.Is<UserBadge>(
            ub => ub.UserId == 100 && ub.BadgeId == 42)), Times.Once);
    }

    #endregion

    #region UpdateProgressAsync — Reward Distribution (VOUCHER)

    [Fact]
    public async Task UpdateProgressAsync_VoucherReward_CreatesNewUserVoucher()
    {
        // Arrange
        var task = MakeUserQuestTask(
            currentValue: 4, targetValue: 5,
            rewardType: "VOUCHER", rewardValue: 10);

        _userQuestRepoMock
            .Setup(r => r.GetInProgressTasksByTypeAsync(100, "REVIEW"))
            .ReturnsAsync(new List<UserQuestTask> { task });
        _userQuestRepoMock
            .Setup(r => r.UpdateUserQuestTaskAsync(It.IsAny<UserQuestTask>()))
            .Returns(Task.CompletedTask);
        _userQuestRepoMock.Setup(r => r.AreAllTasksCompletedAsync(10)).ReturnsAsync(false);
        _userVoucherRepoMock
            .Setup(r => r.GetByUserAndVoucherAsync(100, 10))
            .ReturnsAsync((UserVoucher?)null);
        _userVoucherRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<UserVoucher>()))
            .ReturnsAsync((UserVoucher uv) => uv);

        // Act
        await _sut.UpdateProgressAsync(100, "REVIEW", 1);

        // Assert
        _userVoucherRepoMock.Verify(r => r.CreateAsync(It.Is<UserVoucher>(
            uv => uv.UserId == 100 && uv.VoucherId == 10 && uv.Quantity == 1)), Times.Once);
    }

    [Fact]
    public async Task UpdateProgressAsync_VoucherReward_IncrementsExistingUserVoucher()
    {
        // Arrange
        var task = MakeUserQuestTask(
            currentValue: 4, targetValue: 5,
            rewardType: "VOUCHER", rewardValue: 10);
        var existingVoucher = new UserVoucher
        {
            UserId = 100,
            VoucherId = 10,
            Quantity = 2,
            IsAvailable = true
        };

        _userQuestRepoMock
            .Setup(r => r.GetInProgressTasksByTypeAsync(100, "REVIEW"))
            .ReturnsAsync(new List<UserQuestTask> { task });
        _userQuestRepoMock
            .Setup(r => r.UpdateUserQuestTaskAsync(It.IsAny<UserQuestTask>()))
            .Returns(Task.CompletedTask);
        _userQuestRepoMock.Setup(r => r.AreAllTasksCompletedAsync(10)).ReturnsAsync(false);
        _userVoucherRepoMock
            .Setup(r => r.GetByUserAndVoucherAsync(100, 10))
            .ReturnsAsync(existingVoucher);
        _userVoucherRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<UserVoucher>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateProgressAsync(100, "REVIEW", 1);

        // Assert
        existingVoucher.Quantity.Should().Be(3); // 2 + 1
        _userVoucherRepoMock.Verify(r => r.UpdateAsync(existingVoucher), Times.Once);
        _userVoucherRepoMock.Verify(r => r.CreateAsync(It.IsAny<UserVoucher>()), Times.Never);
    }

    #endregion

    #region UpdateProgressAsync — Quest Completion

    [Fact]
    public async Task UpdateProgressAsync_WhenAllTasksCompleted_MarksQuestCompleted()
    {
        // Arrange
        var task = MakeUserQuestTask(currentValue: 4, targetValue: 5);

        _userQuestRepoMock
            .Setup(r => r.GetInProgressTasksByTypeAsync(100, "REVIEW"))
            .ReturnsAsync(new List<UserQuestTask> { task });
        _userQuestRepoMock
            .Setup(r => r.UpdateUserQuestTaskAsync(It.IsAny<UserQuestTask>()))
            .Returns(Task.CompletedTask);
        _userQuestRepoMock.Setup(r => r.AreAllTasksCompletedAsync(10)).ReturnsAsync(true);

        var userQuest = new UserQuest
        {
            UserQuestId = 10,
            Status = "IN_PROGRESS"
        };
        _userQuestRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(userQuest);
        _userQuestRepoMock
            .Setup(r => r.UpdateUserQuestAsync(It.IsAny<UserQuest>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateProgressAsync(100, "REVIEW", 1);

        // Assert
        userQuest.Status.Should().Be("COMPLETED");
        userQuest.CompletedAt.Should().NotBeNull();
        _userQuestRepoMock.Verify(r => r.UpdateUserQuestAsync(userQuest), Times.Once);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenNotAllTasksCompleted_QuestRemainsInProgress()
    {
        // Arrange
        var task = MakeUserQuestTask(currentValue: 4, targetValue: 5);

        _userQuestRepoMock
            .Setup(r => r.GetInProgressTasksByTypeAsync(100, "REVIEW"))
            .ReturnsAsync(new List<UserQuestTask> { task });
        _userQuestRepoMock
            .Setup(r => r.UpdateUserQuestTaskAsync(It.IsAny<UserQuestTask>()))
            .Returns(Task.CompletedTask);
        _userQuestRepoMock.Setup(r => r.AreAllTasksCompletedAsync(10)).ReturnsAsync(false);

        // Act
        await _sut.UpdateProgressAsync(100, "REVIEW", 1);

        // Assert
        _userQuestRepoMock.Verify(
            r => r.UpdateUserQuestAsync(It.IsAny<UserQuest>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenQuestAlreadyCompleted_DoesNotUpdateAgain()
    {
        // Arrange
        var task = MakeUserQuestTask(currentValue: 4, targetValue: 5);

        _userQuestRepoMock
            .Setup(r => r.GetInProgressTasksByTypeAsync(100, "REVIEW"))
            .ReturnsAsync(new List<UserQuestTask> { task });
        _userQuestRepoMock
            .Setup(r => r.UpdateUserQuestTaskAsync(It.IsAny<UserQuestTask>()))
            .Returns(Task.CompletedTask);
        _userQuestRepoMock.Setup(r => r.AreAllTasksCompletedAsync(10)).ReturnsAsync(true);

        var userQuest = new UserQuest
        {
            UserQuestId = 10,
            Status = "COMPLETED" // already completed
        };
        _userQuestRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(userQuest);

        // Act
        await _sut.UpdateProgressAsync(100, "REVIEW", 1);

        // Assert
        _userQuestRepoMock.Verify(
            r => r.UpdateUserQuestAsync(It.IsAny<UserQuest>()), Times.Never);
    }

    #endregion

    #region UpdateProgressAsync — Reward Not Claimed Twice

    [Fact]
    public async Task UpdateProgressAsync_WhenRewardAlreadyClaimed_DoesNotDistributeAgain()
    {
        // Arrange
        var task = MakeUserQuestTask(
            currentValue: 4, targetValue: 5,
            rewardType: "POINTS", rewardValue: 100);
        task.RewardClaimed = true; // already claimed

        _userQuestRepoMock
            .Setup(r => r.GetInProgressTasksByTypeAsync(100, "REVIEW"))
            .ReturnsAsync(new List<UserQuestTask> { task });
        _userQuestRepoMock
            .Setup(r => r.UpdateUserQuestTaskAsync(It.IsAny<UserQuestTask>()))
            .Returns(Task.CompletedTask);
        _userQuestRepoMock.Setup(r => r.AreAllTasksCompletedAsync(10)).ReturnsAsync(false);

        // Act
        await _sut.UpdateProgressAsync(100, "REVIEW", 1);

        // Assert — no reward distribution calls
        _userRepoMock.Verify(r => r.GetUserById(It.IsAny<int>()), Times.Never);
        _userBadgeRepoMock.Verify(r => r.Create(It.IsAny<UserBadge>()), Times.Never);
        _userVoucherRepoMock.Verify(r => r.CreateAsync(It.IsAny<UserVoucher>()), Times.Never);
    }

    #endregion

    #region UpdateProgressAsync — ORDER_AMOUNT Task Type

    [Fact]
    public async Task UpdateProgressAsync_OrderAmountTask_IncrementsbyOrderTotal()
    {
        // Arrange
        var task = MakeUserQuestTask(
            currentValue: 200000,
            targetValue: 500000,
            taskType: "ORDER_AMOUNT",
            rewardType: "POINTS",
            rewardValue: 200);

        _userQuestRepoMock
            .Setup(r => r.GetInProgressTasksByTypeAsync(100, "ORDER_AMOUNT"))
            .ReturnsAsync(new List<UserQuestTask> { task });
        _userQuestRepoMock
            .Setup(r => r.UpdateUserQuestTaskAsync(It.IsAny<UserQuestTask>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateProgressAsync(100, "ORDER_AMOUNT", 150000);

        // Assert
        task.CurrentValue.Should().Be(350000);
        task.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProgressAsync_OrderAmountTask_CompletesWhenTargetReached()
    {
        // Arrange
        var task = MakeUserQuestTask(
            currentValue: 400000,
            targetValue: 500000,
            taskType: "ORDER_AMOUNT",
            rewardType: "VOUCHER",
            rewardValue: 5);

        _userQuestRepoMock
            .Setup(r => r.GetInProgressTasksByTypeAsync(100, "ORDER_AMOUNT"))
            .ReturnsAsync(new List<UserQuestTask> { task });
        _userQuestRepoMock
            .Setup(r => r.UpdateUserQuestTaskAsync(It.IsAny<UserQuestTask>()))
            .Returns(Task.CompletedTask);
        _userQuestRepoMock.Setup(r => r.AreAllTasksCompletedAsync(10)).ReturnsAsync(false);
        _userVoucherRepoMock
            .Setup(r => r.GetByUserAndVoucherAsync(100, 5))
            .ReturnsAsync((UserVoucher?)null);
        _userVoucherRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<UserVoucher>()))
            .ReturnsAsync((UserVoucher uv) => uv);

        // Act
        await _sut.UpdateProgressAsync(100, "ORDER_AMOUNT", 100000);

        // Assert
        task.CurrentValue.Should().Be(500000);
        task.IsCompleted.Should().BeTrue();
        task.RewardClaimed.Should().BeTrue();
    }

    #endregion
}
