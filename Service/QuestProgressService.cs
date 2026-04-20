using BO.Entities;
using BO.Enums;
using Repository.Interfaces;
using Service.Interfaces;
using Service.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Service
{
    public class QuestProgressService : IQuestProgressService
    {
        private readonly IUserQuestRepository _userQuestRepository;
        private readonly IUserBadgeRepository _userBadgeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserVoucherRepository _userVoucherRepository;
        private readonly IVoucherRepository _voucherRepository;
        private readonly IQuestRepository _questRepository;
        private readonly INotificationService _notificationService;

        // Warning (1) and Silver (2) have no tier-up quest
        private static readonly int[] NoRewardTierIds = { 1, 2 };

        public QuestProgressService(
            IUserQuestRepository userQuestRepository,
            IUserBadgeRepository userBadgeRepository,
            IUserRepository userRepository,
            IUserVoucherRepository userVoucherRepository,
            IVoucherRepository voucherRepository,
            IQuestRepository questRepository,
            INotificationService notificationService)
        {
            _userQuestRepository = userQuestRepository;
            _userBadgeRepository = userBadgeRepository;
            _userRepository = userRepository;
            _userVoucherRepository = userVoucherRepository;
            _voucherRepository = voucherRepository;
            _questRepository = questRepository;
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
                    var taskLabel = userQuestTask.QuestTask.Description ?? userQuestTask.QuestTask.Type.ToString();
                    await _notificationService.NotifyAsync(
                        userId,
                        NotificationType.QuestTaskCompleted,
                        "Nhiệm vụ hoàn thành!",
                        $"Bạn đã hoàn thành nhiệm vụ: {taskLabel}",
                        userQuestTask.QuestTaskId);

                    await CheckAndCompleteQuestAsync(userId, userQuestTask.UserQuestId);
                }
            }
        }

        public async Task HandleTierUpAsync(int userId, int newTierId)
        {
            // No tier-up quest for Warning (1) or Silver (2)
            if (NoRewardTierIds.Contains(newTierId))
                return;

            var quest = await _questRepository.GetActiveTierUpQuestForTierAsync(newTierId);
            if (quest == null)
                return;

            // Idempotency: don't re-grant if already completed
            var existing = await _userQuestRepository.GetByUserAndQuestAnyStatusAsync(userId, quest.QuestId);
            if (existing != null && existing.Status == "COMPLETED")
                return;

            var questTask = quest.QuestTasks.FirstOrDefault(t =>
                t.Type == QuestTaskType.TIER_UP && t.TargetValue == newTierId);
            if (questTask == null)
                return;

            // Create completed UserQuest and UserQuestTask atomically
            var userQuest = new UserQuest
            {
                UserId = userId,
                QuestId = quest.QuestId,
                Status = "COMPLETED",
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
            var created = await _userQuestRepository.CreateAsync(userQuest);

            var userQuestTask = new UserQuestTask
            {
                UserQuestId = created.UserQuestId,
                QuestTaskId = questTask.QuestTaskId,
                CurrentValue = 1,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow,
                RewardClaimed = false
            };
            await _userQuestRepository.AddUserQuestTasksAsync(new System.Collections.Generic.List<UserQuestTask> { userQuestTask });

            // Load back with navigation so rewards are available
            var loaded = await _userQuestRepository.GetByIdAsync(created.UserQuestId);
            var loadedTask = loaded?.UserQuestTasks.FirstOrDefault();
            if (loadedTask != null)
            {
                await DistributeTaskRewardAsync(userId, loadedTask);
                loadedTask.RewardClaimed = true;
                await _userQuestRepository.UpdateUserQuestTaskAsync(loadedTask);
            }

            // Push notification with questTaskId for FE reward deep-link
            await _notificationService.NotifyAsync(
                userId,
                NotificationType.QuestCompleted,
                "Lên cấp độ!",
                $"Chúc mừng! Bạn đã đạt cấp độ mới và nhận được phần thưởng.",
                loadedTask?.QuestTaskId ?? questTask.QuestTaskId);
        }

        private async Task DistributeTaskRewardAsync(int userId, UserQuestTask userQuestTask)
        {
            // Guard: don't re-distribute if already claimed
            if (userQuestTask.RewardClaimed)
                return;

            var rewards = userQuestTask.QuestTask?.QuestTaskRewards;
            if (rewards == null || !rewards.Any())
                return;

            foreach (var reward in rewards)
            {
                switch (reward.RewardType)
                {
                    case QuestRewardType.BADGE:
                        var alreadyHasBadge = await _userBadgeRepository.Exists(userId, reward.RewardValue);
                        if (!alreadyHasBadge)
                        {
                            await _userBadgeRepository.Create(new UserBadge
                            {
                                UserId = userId,
                                BadgeId = reward.RewardValue,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                        break;

                    case QuestRewardType.POINTS:
                        var user = await _userRepository.GetUserById(userId);
                        if (user != null)
                        {
                            user.Point += reward.RewardValue * reward.Quantity;
                            await _userRepository.UpdateAsync(user);
                        }
                        break;

                    case QuestRewardType.VOUCHER:
                        var voucher = await _voucherRepository.GetByIdAsync(reward.RewardValue);
                        if (voucher != null && VoucherRules.HasRemainingQuantity(voucher))
                        {
                            int grantQty = VoucherRules.HasUnlimitedQuantity(voucher)
                                ? reward.Quantity
                                : Math.Min(reward.Quantity, voucher.Quantity - voucher.UsedQuantity);
                            voucher.UsedQuantity += grantQty;
                            await _voucherRepository.UpdateAsync(voucher);

                            var existingUserVoucher = await _userVoucherRepository.GetByUserAndVoucherAsync(userId, reward.RewardValue);
                            if (existingUserVoucher != null)
                            {
                                existingUserVoucher.Quantity += grantQty;
                                await _userVoucherRepository.UpdateAsync(existingUserVoucher);
                            }
                            else
                            {
                                await _userVoucherRepository.CreateAsync(new UserVoucher
                                {
                                    UserId = userId,
                                    VoucherId = reward.RewardValue,
                                    Quantity = grantQty,
                                    IsAvailable = true
                                });
                            }

                            if (!VoucherRules.HasUnlimitedQuantity(voucher) && VoucherRules.IsOutOfStock(voucher))
                            {
                                userQuestTask.QuestTask.IsActive = false;
                                await _questRepository.UpdateTaskAsync(userQuestTask.QuestTask);
                            }
                        }
                        break;
                }
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
                userQuest.QuestId);
        }
    }
}
