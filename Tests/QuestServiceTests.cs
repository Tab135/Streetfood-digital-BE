using BO.Common;
using BO.DTO.Quest;
using BO.Entities;
using FluentAssertions;
using Moq;
using Repository.Interfaces;
using Service;

namespace Tests;

public class QuestServiceTests
{
    private readonly Mock<IQuestRepository> _questRepoMock;
    private readonly Mock<IUserQuestRepository> _userQuestRepoMock;
    private readonly Mock<ICampaignRepository> _campaignRepoMock;
    private readonly Mock<IBadgeRepository> _badgeRepoMock;
    private readonly Mock<IVoucherRepository> _voucherRepoMock;
    private readonly QuestService _sut;

    public QuestServiceTests()
    {
        _questRepoMock = new Mock<IQuestRepository>();
        _userQuestRepoMock = new Mock<IUserQuestRepository>();
        _campaignRepoMock = new Mock<ICampaignRepository>();
        _badgeRepoMock = new Mock<IBadgeRepository>();
        _voucherRepoMock = new Mock<IVoucherRepository>();

        _sut = new QuestService(
            _questRepoMock.Object,
            _userQuestRepoMock.Object,
            _campaignRepoMock.Object,
            _badgeRepoMock.Object,
            _voucherRepoMock.Object);
    }

    #region Helper Methods

    private static CreateQuestDto MakeCreateDto(
        int? campaignId = null,
        List<CreateQuestTaskDto>? tasks = null)
    {
        return new CreateQuestDto
        {
            Title = "Test Quest",
            Description = "Test Description",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            IsActive = true,
            CampaignId = campaignId,
            Tasks = tasks ?? new List<CreateQuestTaskDto>
            {
                new()
                {
                    Type = "REVIEW",
                    TargetValue = 5,
                    Description = "Write 5 reviews",
                    RewardType = "POINTS",
                    RewardValue = 100
                }
            }
        };
    }

    private static Quest MakeQuest(int questId = 1, bool isActive = true, DateTime? endDate = null)
    {
        return new Quest
        {
            QuestId = questId,
            Title = "Test Quest",
            Description = "Test Description",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = endDate ?? DateTime.UtcNow.AddDays(30),
            IsActive = isActive,
            QuestTasks = new List<QuestTask>
            {
                new()
                {
                    QuestTaskId = 1,
                    QuestId = questId,
                    Type = "REVIEW",
                    TargetValue = 5,
                    Description = "Write 5 reviews",
                    RewardType = "POINTS",
                    RewardValue = 100
                }
            }
        };
    }

    #endregion

    #region CreateQuestAsync

    [Fact]
    public async Task CreateQuestAsync_WithValidData_ReturnsQuestResponse()
    {
        // Arrange
        var dto = MakeCreateDto();
        _questRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Quest>()))
            .ReturnsAsync((Quest q) =>
            {
                q.QuestId = 1;
                return q;
            });

        // Act
        var result = await _sut.CreateQuestAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(dto.Title);
        result.TaskCount.Should().Be(1);
        result.Tasks.Should().HaveCount(1);
        _questRepoMock.Verify(r => r.CreateAsync(It.IsAny<Quest>()), Times.Once);
    }

    [Fact]
    public async Task CreateQuestAsync_WithNoTasks_ThrowsException()
    {
        // Arrange
        var dto = MakeCreateDto(tasks: new List<CreateQuestTaskDto>());

        // Act
        var act = () => _sut.CreateQuestAsync(dto);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("At least one task is required");
    }

    [Fact]
    public async Task CreateQuestAsync_WithNullTasks_ThrowsException()
    {
        // Arrange
        var dto = MakeCreateDto();
        dto.Tasks = null!;

        // Act
        var act = () => _sut.CreateQuestAsync(dto);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("At least one task is required");
    }

    [Fact]
    public async Task CreateQuestAsync_WithInvalidCampaignId_ThrowsException()
    {
        // Arrange
        var dto = MakeCreateDto(campaignId: 999);
        _campaignRepoMock
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Campaign?)null);

        // Act
        var act = () => _sut.CreateQuestAsync(dto);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Campaign not found");
    }

    [Fact]
    public async Task CreateQuestAsync_WithValidCampaignId_Succeeds()
    {
        // Arrange
        var dto = MakeCreateDto(campaignId: 1);
        _campaignRepoMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Campaign { CampaignId = 1 });
        _questRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Quest>()))
            .ReturnsAsync((Quest q) => { q.QuestId = 1; return q; });

        // Act
        var result = await _sut.CreateQuestAsync(dto);

        // Assert
        result.CampaignId.Should().Be(1);
    }

    [Fact]
    public async Task CreateQuestAsync_WithBadgeReward_ValidatesBadgeExists()
    {
        // Arrange
        var dto = MakeCreateDto(tasks: new List<CreateQuestTaskDto>
        {
            new() { Type = "REVIEW", TargetValue = 5, RewardType = "BADGE", RewardValue = 42 }
        });
        _badgeRepoMock.Setup(r => r.Exists(42)).ReturnsAsync(true);
        _questRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Quest>()))
            .ReturnsAsync((Quest q) => { q.QuestId = 1; return q; });

        // Act
        var result = await _sut.CreateQuestAsync(dto);

        // Assert
        result.Should().NotBeNull();
        _badgeRepoMock.Verify(r => r.Exists(42), Times.Once);
    }

    [Fact]
    public async Task CreateQuestAsync_WithInvalidBadgeId_ThrowsException()
    {
        // Arrange
        var dto = MakeCreateDto(tasks: new List<CreateQuestTaskDto>
        {
            new() { Type = "REVIEW", TargetValue = 5, RewardType = "BADGE", RewardValue = 999 }
        });
        _badgeRepoMock.Setup(r => r.Exists(999)).ReturnsAsync(false);

        // Act
        var act = () => _sut.CreateQuestAsync(dto);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Badge with ID 999 not found");
    }

    [Fact]
    public async Task CreateQuestAsync_WithVoucherReward_ValidatesVoucherExists()
    {
        // Arrange
        var dto = MakeCreateDto(tasks: new List<CreateQuestTaskDto>
        {
            new() { Type = "VISIT", TargetValue = 3, RewardType = "VOUCHER", RewardValue = 10 }
        });
        _voucherRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Voucher { VoucherId = 10 });
        _questRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Quest>()))
            .ReturnsAsync((Quest q) => { q.QuestId = 1; return q; });

        // Act
        var result = await _sut.CreateQuestAsync(dto);

        // Assert
        result.Should().NotBeNull();
        _voucherRepoMock.Verify(r => r.GetByIdAsync(10), Times.Once);
    }

    [Fact]
    public async Task CreateQuestAsync_WithInvalidVoucherId_ThrowsException()
    {
        // Arrange
        var dto = MakeCreateDto(tasks: new List<CreateQuestTaskDto>
        {
            new() { Type = "VISIT", TargetValue = 3, RewardType = "VOUCHER", RewardValue = 999 }
        });
        _voucherRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Voucher?)null);

        // Act
        var act = () => _sut.CreateQuestAsync(dto);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Voucher with ID 999 not found");
    }

    [Fact]
    public async Task CreateQuestAsync_WithInvalidRewardType_ThrowsException()
    {
        // Arrange
        var dto = MakeCreateDto(tasks: new List<CreateQuestTaskDto>
        {
            new() { Type = "REVIEW", TargetValue = 5, RewardType = "GOLD", RewardValue = 100 }
        });

        // Act
        var act = () => _sut.CreateQuestAsync(dto);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Invalid reward type: GOLD*");
    }

    [Fact]
    public async Task CreateQuestAsync_WithMultipleTasks_CreatesAllTasks()
    {
        // Arrange
        var dto = MakeCreateDto(tasks: new List<CreateQuestTaskDto>
        {
            new() { Type = "REVIEW", TargetValue = 5, RewardType = "POINTS", RewardValue = 100 },
            new() { Type = "ORDER_AMOUNT", TargetValue = 500000, RewardType = "POINTS", RewardValue = 200 },
            new() { Type = "VISIT", TargetValue = 3, RewardType = "POINTS", RewardValue = 50 }
        });
        _questRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Quest>()))
            .ReturnsAsync((Quest q) => { q.QuestId = 1; return q; });

        // Act
        var result = await _sut.CreateQuestAsync(dto);

        // Assert
        result.TaskCount.Should().Be(3);
        result.Tasks.Should().HaveCount(3);
    }

    #endregion

    #region UpdateQuestAsync

    [Fact]
    public async Task UpdateQuestAsync_WithNonExistentQuest_ThrowsException()
    {
        // Arrange
        _questRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Quest?)null);

        // Act
        var act = () => _sut.UpdateQuestAsync(999, new UpdateQuestDto());

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Quest with ID 999 not found");
    }

    [Fact]
    public async Task UpdateQuestAsync_WithPartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var quest = MakeQuest();
        var originalTitle = quest.Title;
        _questRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quest);
        _questRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Quest>())).Returns(Task.CompletedTask);

        var dto = new UpdateQuestDto { Title = "Updated Title" };

        // Act
        var result = await _sut.UpdateQuestAsync(1, dto);

        // Assert
        result.Title.Should().Be("Updated Title");
        result.Description.Should().Be(quest.Description); // unchanged
    }

    [Fact]
    public async Task UpdateQuestAsync_WithNewTasks_RemovesOldAndAddsNew()
    {
        // Arrange
        var quest = MakeQuest();
        _questRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quest);
        _questRepoMock.Setup(r => r.RemoveTasksAsync(It.IsAny<List<QuestTask>>())).Returns(Task.CompletedTask);
        _questRepoMock.Setup(r => r.AddTasksAsync(It.IsAny<List<QuestTask>>())).Returns(Task.CompletedTask);
        _questRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Quest>())).Returns(Task.CompletedTask);

        var dto = new UpdateQuestDto
        {
            Tasks = new List<CreateQuestTaskDto>
            {
                new() { Type = "SHARE", TargetValue = 10, RewardType = "POINTS", RewardValue = 50 }
            }
        };

        // Act
        var result = await _sut.UpdateQuestAsync(1, dto);

        // Assert
        _questRepoMock.Verify(r => r.RemoveTasksAsync(It.IsAny<List<QuestTask>>()), Times.Once);
        _questRepoMock.Verify(r => r.AddTasksAsync(It.IsAny<List<QuestTask>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateQuestAsync_WithInvalidCampaignId_ThrowsException()
    {
        // Arrange
        var quest = MakeQuest();
        _questRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quest);
        _campaignRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Campaign?)null);

        var dto = new UpdateQuestDto { CampaignId = 999 };

        // Act
        var act = () => _sut.UpdateQuestAsync(1, dto);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Campaign not found");
    }

    #endregion

    #region DeleteQuestAsync

    [Fact]
    public async Task DeleteQuestAsync_WithEnrolledUsers_ThrowsException()
    {
        // Arrange
        _questRepoMock.Setup(r => r.HasEnrolledUsersAsync(1)).ReturnsAsync(true);

        // Act
        var act = () => _sut.DeleteQuestAsync(1);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Cannot delete quest while users are enrolled");
    }

    [Fact]
    public async Task DeleteQuestAsync_WithNoEnrolledUsers_Succeeds()
    {
        // Arrange
        _questRepoMock.Setup(r => r.HasEnrolledUsersAsync(1)).ReturnsAsync(false);
        _questRepoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteQuestAsync(1);

        // Assert
        result.Should().BeTrue();
        _questRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    #endregion

    #region GetQuestByIdAsync

    [Fact]
    public async Task GetQuestByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var quest = MakeQuest();
        _questRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quest);

        // Act
        var result = await _sut.GetQuestByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.QuestId.Should().Be(1);
        result.Title.Should().Be("Test Quest");
    }

    [Fact]
    public async Task GetQuestByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _questRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Quest?)null);

        // Act
        var result = await _sut.GetQuestByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetQuestsAsync / GetPublicQuestsAsync

    [Fact]
    public async Task GetQuestsAsync_ReturnsPaginatedResponse()
    {
        // Arrange
        var quests = new List<Quest> { MakeQuest(1), MakeQuest(2) };
        _questRepoMock
            .Setup(r => r.GetQuestsAsync(null, null, 1, 10))
            .ReturnsAsync((quests, 2));

        var query = new QuestQueryDto { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _sut.GetQuestsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPublicQuestsAsync_ReturnsPaginatedResponse()
    {
        // Arrange
        var quests = new List<Quest> { MakeQuest(1) };
        _questRepoMock
            .Setup(r => r.GetPublicQuestsAsync(1, 10))
            .ReturnsAsync((quests, 1));

        var query = new QuestQueryDto { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _sut.GetPublicQuestsAsync(query);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    #endregion

    #region EnrollInQuestAsync

    [Fact]
    public async Task EnrollInQuestAsync_WithValidQuest_CreatesUserQuestAndTasks()
    {
        // Arrange
        var quest = MakeQuest();
        _questRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quest);
        _userQuestRepoMock.Setup(r => r.GetByUserAndQuestAsync(100, 1)).ReturnsAsync((UserQuest?)null);
        _userQuestRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<UserQuest>()))
            .ReturnsAsync((UserQuest uq) => { uq.UserQuestId = 10; return uq; });
        _userQuestRepoMock.Setup(r => r.AddUserQuestTasksAsync(It.IsAny<List<UserQuestTask>>())).Returns(Task.CompletedTask);

        var loadedUserQuest = new UserQuest
        {
            UserQuestId = 10,
            UserId = 100,
            QuestId = 1,
            Status = "IN_PROGRESS",
            Quest = quest,
            UserQuestTasks = new List<UserQuestTask>
            {
                new()
                {
                    UserQuestTaskId = 1,
                    UserQuestId = 10,
                    QuestTaskId = 1,
                    CurrentValue = 0,
                    IsCompleted = false,
                    QuestTask = quest.QuestTasks.First()
                }
            }
        };
        _userQuestRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(loadedUserQuest);

        // Act
        var result = await _sut.EnrollInQuestAsync(100, 1);

        // Assert
        result.Should().NotBeNull();
        result.QuestId.Should().Be(1);
        result.Status.Should().Be("IN_PROGRESS");
        result.TotalTasks.Should().Be(1);
        result.CompletedTasks.Should().Be(0);
        _userQuestRepoMock.Verify(r => r.CreateAsync(It.IsAny<UserQuest>()), Times.Once);
        _userQuestRepoMock.Verify(r => r.AddUserQuestTasksAsync(It.IsAny<List<UserQuestTask>>()), Times.Once);
    }

    [Fact]
    public async Task EnrollInQuestAsync_WhenQuestNotFound_ThrowsException()
    {
        // Arrange
        _questRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Quest?)null);

        // Act
        var act = () => _sut.EnrollInQuestAsync(100, 999);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Quest not found");
    }

    [Fact]
    public async Task EnrollInQuestAsync_WhenQuestInactive_ThrowsException()
    {
        // Arrange
        var quest = MakeQuest(isActive: false);
        _questRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quest);

        // Act
        var act = () => _sut.EnrollInQuestAsync(100, 1);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Quest is not available");
    }

    [Fact]
    public async Task EnrollInQuestAsync_WhenQuestExpired_ThrowsException()
    {
        // Arrange
        var quest = MakeQuest(endDate: DateTime.UtcNow.AddDays(-1));
        _questRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quest);

        // Act
        var act = () => _sut.EnrollInQuestAsync(100, 1);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Quest has ended");
    }

    [Fact]
    public async Task EnrollInQuestAsync_WhenAlreadyEnrolled_ThrowsException()
    {
        // Arrange
        var quest = MakeQuest();
        _questRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quest);
        _userQuestRepoMock
            .Setup(r => r.GetByUserAndQuestAsync(100, 1))
            .ReturnsAsync(new UserQuest { UserQuestId = 5 });

        // Act
        var act = () => _sut.EnrollInQuestAsync(100, 1);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("You are already enrolled in this quest");
    }

    #endregion

    #region GetMyQuestsAsync

    [Fact]
    public async Task GetMyQuestsAsync_ReturnsUserQuests()
    {
        // Arrange
        var quest = MakeQuest();
        var userQuests = new List<UserQuest>
        {
            new()
            {
                UserQuestId = 1,
                UserId = 100,
                QuestId = 1,
                Status = "IN_PROGRESS",
                StartedAt = DateTime.UtcNow,
                Quest = quest,
                UserQuestTasks = new List<UserQuestTask>
                {
                    new()
                    {
                        UserQuestTaskId = 1,
                        QuestTaskId = 1,
                        CurrentValue = 2,
                        IsCompleted = false,
                        QuestTask = quest.QuestTasks.First()
                    }
                }
            }
        };
        _userQuestRepoMock.Setup(r => r.GetByUserIdAsync(100, null)).ReturnsAsync(userQuests);

        // Act
        var result = await _sut.GetMyQuestsAsync(100, null);

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be("IN_PROGRESS");
        result[0].CompletedTasks.Should().Be(0);
    }

    [Fact]
    public async Task GetMyQuestsAsync_WithStatusFilter_PassesStatusToRepo()
    {
        // Arrange
        _userQuestRepoMock.Setup(r => r.GetByUserIdAsync(100, "COMPLETED")).ReturnsAsync(new List<UserQuest>());

        // Act
        var result = await _sut.GetMyQuestsAsync(100, "COMPLETED");

        // Assert
        result.Should().BeEmpty();
        _userQuestRepoMock.Verify(r => r.GetByUserIdAsync(100, "COMPLETED"), Times.Once);
    }

    #endregion
}
