using BO.Entities;
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

        public QuestProgressService(
            IUserQuestRepository userQuestRepository,
            IUserBadgeRepository userBadgeRepository,
            IUserRepository userRepository,
            IUserVoucherRepository userVoucherRepository)
        {
            _userQuestRepository = userQuestRepository;
            _userBadgeRepository = userBadgeRepository;
            _userRepository = userRepository;
            _userVoucherRepository = userVoucherRepository;
        }

        public async Task UpdateProgressAsync(int userId, string taskType, int incrementValue)
        {
            var matchingTasks = await _userQuestRepository.GetInProgressTasksByTypeAsync(userId, taskType);

            foreach (var userQuestTask in matchingTasks)
            {
                userQuestTask.CurrentValue += incrementValue;

                if (userQuestTask.CurrentValue >= userQuestTask.QuestTask.TargetValue)
                {
                    userQuestTask.IsCompleted = true;
                    userQuestTask.CompletedAt = DateTime.UtcNow;

                    // Distribute reward for this task
                    if (!userQuestTask.RewardClaimed)
                    {
                        await DistributeTaskRewardAsync(userId, userQuestTask);
                        userQuestTask.RewardClaimed = true;
                    }
                }

                await _userQuestRepository.UpdateUserQuestTaskAsync(userQuestTask);

                // Check if all tasks for this quest are completed
                if (userQuestTask.IsCompleted)
                {
                    await CheckAndCompleteQuestAsync(userQuestTask.UserQuestId);
                }
            }
        }

        private async Task DistributeTaskRewardAsync(int userId, UserQuestTask userQuestTask)
        {
            var rewardType = userQuestTask.QuestTask.RewardType.ToUpper();
            var rewardValue = userQuestTask.QuestTask.RewardValue;

            switch (rewardType)
            {
                case "BADGE":
                    var userBadge = new UserBadge
                    {
                        UserId = userId,
                        BadgeId = rewardValue,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _userBadgeRepository.Create(userBadge);
                    break;

                case "POINTS":
                    var user = await _userRepository.GetUserById(userId);
                    if (user != null)
                    {
                        user.Point += rewardValue;
                        await _userRepository.UpdateAsync(user);
                    }
                    break;

                case "VOUCHER":
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

        private async Task CheckAndCompleteQuestAsync(int userQuestId)
        {
            var allCompleted = await _userQuestRepository.AreAllTasksCompletedAsync(userQuestId);
            if (allCompleted)
            {
                var userQuest = await _userQuestRepository.GetByIdAsync(userQuestId);
                if (userQuest != null && userQuest.Status == "IN_PROGRESS")
                {
                    userQuest.Status = "COMPLETED";
                    userQuest.CompletedAt = DateTime.UtcNow;
                    await _userQuestRepository.UpdateUserQuestAsync(userQuest);
                }
            }
        }
    }
}
