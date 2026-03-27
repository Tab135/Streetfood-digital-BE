using BO.Entities;
using BO.Enums;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace Service
{
    public class QuestProgressService : IQuestProgressService
    {
        private readonly IUserQuestRepository _userQuestRepository;
        private readonly IUserBadgeRepository _userBadgeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserVoucherRepository _userVoucherRepository;
        private readonly INotificationService _notificationService;

        public QuestProgressService(
            IUserQuestRepository userQuestRepository,
            IUserBadgeRepository userBadgeRepository,
            IUserRepository userRepository,
            IUserVoucherRepository userVoucherRepository,
            INotificationService notificationService)
        {
            _userQuestRepository = userQuestRepository;
            _userBadgeRepository = userBadgeRepository;
            _userRepository = userRepository;
            _userVoucherRepository = userVoucherRepository;
            _notificationService = notificationService;
        }

        public async Task UpdateProgressAsync(int userId, QuestTaskType taskType, int incrementValue)
        {
            var matchingTasks = await _userQuestRepository.GetInProgressTasksByTypeAsync(userId, taskType);

            foreach (var userQuestTask in matchingTasks)
            {
                userQuestTask.CurrentValue += incrementValue;

                if (userQuestTask.CurrentValue >= userQuestTask.QuestTask.TargetValue)
                {
                    userQuestTask.IsCompleted = true;
                    userQuestTask.CompletedAt = DateTime.UtcNow;

                    if (!userQuestTask.RewardClaimed)
                    {
                        await DistributeTaskRewardAsync(userId, userQuestTask);
                        userQuestTask.RewardClaimed = true;
                    }
                }

                await _userQuestRepository.UpdateUserQuestTaskAsync(userQuestTask);

                if (userQuestTask.IsCompleted)
                {
                    // Notify task completion
                    var taskLabel = userQuestTask.QuestTask.Description ?? userQuestTask.QuestTask.Type.ToString();
                    await _notificationService.NotifyAsync(
                        userId,
                        NotificationType.QuestTaskCompleted,
                        "Nhiệm vụ hoàn thành!",
                        $"Bạn đã hoàn thành nhiệm vụ: {taskLabel}",
                        userQuestTask.UserQuestId);

                    await CheckAndCompleteQuestAsync(userId, userQuestTask.UserQuestId);
                }
            }
        }

        private async Task DistributeTaskRewardAsync(int userId, UserQuestTask userQuestTask)
        {
            var rewardType = userQuestTask.QuestTask.RewardType;
            var rewardValue = userQuestTask.QuestTask.RewardValue;

            switch (rewardType)
            {
                case QuestRewardType.BADGE:
                    var userBadge = new UserBadge
                    {
                        UserId = userId,
                        BadgeId = rewardValue,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _userBadgeRepository.Create(userBadge);
                    break;

                case QuestRewardType.POINTS:
                    var user = await _userRepository.GetUserById(userId);
                    if (user != null)
                    {
                        user.Point += rewardValue;
                        await _userRepository.UpdateAsync(user);
                    }
                    break;

                case QuestRewardType.VOUCHER:
                    var existingUserVoucher = await _userVoucherRepository.GetByUserAndVoucherAsync(userId, rewardValue);
                    if (existingUserVoucher != null)
                    {
                        existingUserVoucher.Quantity += 1;
                        await _userVoucherRepository.UpdateAsync(existingUserVoucher);
                    }
                    else
                    {
                        var userVoucher = new UserVoucher
                        {
                            UserId = userId,
                            VoucherId = rewardValue,
                            Quantity = 1,
                            IsAvailable = true
                        };
                        await _userVoucherRepository.CreateAsync(userVoucher);
                    }
                    break;
            }
        }

        private async Task CheckAndCompleteQuestAsync(int userId, int userQuestId)
        {
            var allCompleted = await _userQuestRepository.AreAllTasksCompletedAsync(userQuestId);
            if (!allCompleted) return;

            var userQuest = await _userQuestRepository.GetByIdAsync(userQuestId);
            if (userQuest == null || userQuest.Status != "IN_PROGRESS") return;

            userQuest.Status = "COMPLETED";
            userQuest.CompletedAt = DateTime.UtcNow;
            await _userQuestRepository.UpdateUserQuestAsync(userQuest);

            await _notificationService.NotifyAsync(
                userId,
                NotificationType.QuestCompleted,
                "Thử thách hoàn tất!",
                $"Chúc mừng! Bạn đã hoàn tất thử thách \"{userQuest.Quest.Title}\"",
                userQuestId);
        }
    }
}
