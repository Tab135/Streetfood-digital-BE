using BO.Common;
using BO.DTO.Quest;
using BO.DTO.Users;
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
    public class QuestService : IQuestService
    {
        private readonly IQuestRepository _questRepository;
        private readonly IUserQuestRepository _userQuestRepository;
        private readonly ICampaignRepository _campaignRepository;
        private readonly IBadgeRepository _badgeRepository;
        private readonly IVoucherRepository _voucherRepository;
        private readonly IUserVoucherRepository _userVoucherRepository;

        public QuestService(
            IQuestRepository questRepository,
            IUserQuestRepository userQuestRepository,
            ICampaignRepository campaignRepository,
            IBadgeRepository badgeRepository,
            IVoucherRepository voucherRepository,
            IUserVoucherRepository userVoucherRepository)
        {
            _questRepository = questRepository;
            _userQuestRepository = userQuestRepository;
            _campaignRepository = campaignRepository;
            _badgeRepository = badgeRepository;
            _voucherRepository = voucherRepository;
            _userVoucherRepository = userVoucherRepository;
        }

        public async Task<QuestResponseDto> CreateQuestAsync(CreateQuestDto dto)
        {
            if (dto.Tasks == null || dto.Tasks.Count == 0)
                throw new DomainExceptions("Cần có ít nhất một nhiệm vụ");

            // TIER_UP quest validations
            bool hasTierUpTask = dto.Tasks.Any(t => t.Type == QuestTaskType.TIER_UP);
            bool requiresEnrollment = !hasTierUpTask;

            if (hasTierUpTask)
            {
                if (dto.Tasks.Count > 1)
                    throw new DomainExceptions("Quest TIER_UP phải có đúng một nhiệm vụ.");

                // Validate no duplicate active TIER_UP quest for same tier
                int targetTierId = dto.Tasks[0].TargetValue;
                if (await _questRepository.HasActiveTierUpQuestForTierAsync(targetTierId))
                    throw new DomainExceptions($"Đã tồn tại quest TIER_UP đang hoạt động cho cấp độ này.");
            }

            // Validate each task has at least one reward
            foreach (var taskDto in dto.Tasks)
            {
                if (taskDto.Rewards == null || taskDto.Rewards.Count == 0)
                    throw new DomainExceptions("Mỗi nhiệm vụ phải có ít nhất một phần thưởng.");

                foreach (var reward in taskDto.Rewards)
                    await ValidateTaskRewardAsync(reward.RewardType, reward.RewardValue);
            }

            ValidateStandaloneCampaignConsistency(dto.IsStandalone, dto.CampaignId);

            if (dto.CampaignId.HasValue)
            {
                var campaign = await _campaignRepository.GetByIdAsync(dto.CampaignId.Value);
                if (campaign == null)
                    throw new DomainExceptions("Không tìm thấy chiến dịch");
                if (campaign.CreatedByVendorId != null)
                    throw new DomainExceptions("Không thể liên kết quest với chiến dịch của cửa hàng. Chỉ chiến dịch hệ thống mới hỗ trợ quest.");

                if (await _questRepository.ExistsByCampaignIdAsync(dto.CampaignId.Value))
                    throw new DomainExceptions("Chiến dịch này đã có quest. Mỗi chiến dịch chỉ có thể có một quest.");
            }

            var quest = new Quest
            {
                Title = dto.Title,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                IsActive = dto.IsActive,
                IsStandalone = dto.IsStandalone,
                RequiresEnrollment = requiresEnrollment,
                CampaignId = dto.CampaignId,
                QuestTasks = dto.Tasks.Select(t => new QuestTask
                {
                    Type = t.Type,
                    TargetValue = t.TargetValue,
                    Description = t.Description,
                    QuestTaskRewards = t.Rewards.Select(r => new QuestTaskReward
                    {
                        RewardType = r.RewardType,
                        RewardValue = r.RewardValue,
                        Quantity = r.Quantity
                    }).ToList()
                }).ToList()
            };

            var created = await _questRepository.CreateAsync(quest);
            return await MapToResponseDtoAsync(created);
        }

        public async Task<QuestResponseDto> UpdateQuestAsync(int questId, UpdateQuestDto dto)
        {
            var quest = await _questRepository.GetByIdAsync(questId);
            if (quest == null)
                throw new DomainExceptions($"Không tìm thấy quest với ID {questId}");

            // Determine effective standalone/campaign values after update
            bool effectiveIsStandalone = dto.IsStandalone ?? quest.IsStandalone;
            int? effectiveCampaignId = dto.CampaignId ?? quest.CampaignId;

            ValidateStandaloneCampaignConsistency(effectiveIsStandalone, effectiveCampaignId);

            if (!string.IsNullOrEmpty(dto.Title))
                quest.Title = dto.Title;
            if (dto.Description != null)
                quest.Description = dto.Description;
            if (dto.ImageUrl != null)
                quest.ImageUrl = dto.ImageUrl;
            if (dto.IsActive.HasValue)
            {
                var hasUsers = await _questRepository.HasEnrolledUsersAsync(questId);
                if (hasUsers)
                    throw new DomainExceptions("Không thể cập nhật trạng thái hoạt động khi đã có người dùng tham gia quest.");
                quest.IsActive = dto.IsActive.Value;
            }
            if (dto.IsStandalone.HasValue)
                quest.IsStandalone = dto.IsStandalone.Value;

            if (dto.CampaignId.HasValue)
            {
                var campaign = await _campaignRepository.GetByIdAsync(dto.CampaignId.Value);
                if (campaign == null)
                    throw new DomainExceptions("Không tìm thấy chiến dịch");
                if (campaign.CreatedByVendorId != null)
                    throw new DomainExceptions("Không thể liên kết quest với chiến dịch của cửa hàng. Chỉ chiến dịch hệ thống mới hỗ trợ quest.");

                if (await _questRepository.ExistsByCampaignIdAsync(dto.CampaignId.Value, excludeQuestId: questId))
                    throw new DomainExceptions("Chiến dịch này đã có quest. Mỗi chiến dịch chỉ có thể có một quest.");

                quest.CampaignId = dto.CampaignId.Value;
            }

            await _questRepository.UpdateAsync(quest);
            return await MapToResponseDtoAsync(quest);
        }

        public async Task<QuestResponseDto> ReplaceQuestTasksAsync(int questId, List<CreateQuestTaskDto> tasks)
        {
            var quest = await _questRepository.GetByIdAsync(questId)
                ?? throw new DomainExceptions($"Không tìm thấy quest với ID {questId}");

            var hasUsers = await _questRepository.HasEnrolledUsersAsync(questId);
            if (hasUsers)
                throw new DomainExceptions("Không thể thay đổi nhiệm vụ khi đã có người dùng tham gia quest.");

            if (tasks == null || tasks.Count == 0)
                throw new DomainExceptions("Cần có ít nhất một nhiệm vụ.");

            foreach (var taskDto in tasks)
            {
                if (taskDto.Rewards == null || taskDto.Rewards.Count == 0)
                    throw new DomainExceptions("Mỗi nhiệm vụ phải có ít nhất một phần thưởng.");

                foreach (var reward in taskDto.Rewards)
                    await ValidateTaskRewardAsync(reward.RewardType, reward.RewardValue);
            }
            var incomingVoucherIds = tasks
                .SelectMany(t => t.Rewards)
                .Where(r => r.RewardType == QuestRewardType.VOUCHER)
                .Select(r => r.RewardValue)
                .ToHashSet();

            var tasksWithVouchersToRemove = quest.QuestTasks.Select(t => new QuestTask
            {
                QuestTaskRewards = t.QuestTaskRewards
                    .Where(r => r.RewardType == QuestRewardType.VOUCHER && !incomingVoucherIds.Contains(r.RewardValue))
                    .ToList()
            }).ToList();

            await RemoveVouchersFromTasksAsync(tasksWithVouchersToRemove);

            await _questRepository.RemoveTasksAsync([.. quest.QuestTasks]);
            var newTasks = tasks.Select(t => new QuestTask
            {
                QuestId = questId,
                Type = t.Type,
                TargetValue = t.TargetValue,
                Description = t.Description,
                QuestTaskRewards = [.. t.Rewards.Select(r => new QuestTaskReward
                {
                    RewardType = r.RewardType,
                    RewardValue = r.RewardValue,
                    Quantity = r.Quantity
                })]
            }).ToList();
            await _questRepository.AddTasksAsync(newTasks);
            quest.QuestTasks = newTasks;

            quest.RequiresEnrollment = !quest.QuestTasks.Any(t => t.Type == QuestTaskType.TIER_UP);

            await _questRepository.UpdateAsync(quest);
            return await MapToResponseDtoAsync(quest);
        }

        public async Task<bool> DeleteQuestAsync(int questId)
        {
            var hasUsers = await _questRepository.HasEnrolledUsersAsync(questId);
            if (hasUsers)
                throw new DomainExceptions("Không thể xóa quest khi vẫn còn người dùng đang tham gia");

            var quest = await _questRepository.GetByIdAsync(questId);
            if (quest != null)
            {
                await RemoveVouchersFromTasksAsync(quest.QuestTasks);
            }

            return await _questRepository.DeleteAsync(questId);
        }

        public async Task<QuestResponseDto?> GetQuestByIdAsync(int questId)
        {
            var quest = await _questRepository.GetByIdAsync(questId);
            return quest == null ? null : await MapToResponseDtoAsync(quest);
        }

        public async Task<QuestTaskResponseDto?> GetQuestTaskByIdAsync(int questTaskId)
        {
            var task = await _questRepository.GetTaskByIdAsync(questTaskId);
            if (task == null) return null;
            return new QuestTaskResponseDto
            {
                QuestTaskId = task.QuestTaskId,
                QuestId = task.QuestId,
                Type = task.Type,
                TargetValue = task.TargetValue,
                Description = task.Description,
                Rewards = task.QuestTaskRewards.Select(r => new QuestTaskRewardDto
                {
                    QuestTaskRewardId = r.QuestTaskRewardId,
                    RewardType = r.RewardType,
                    RewardValue = r.RewardValue,
                    Quantity = r.Quantity
                }).ToList()
            };
        }

        public async Task<PaginatedResponse<QuestResponseDto>> GetQuestsAsync(QuestQueryDto query)
        {
            var (items, totalCount) = await _questRepository.GetQuestsAsync(
                query.IsActive, query.CampaignId, query.PageNumber, query.PageSize);

            var dtos = await MapToResponseDtosAsync(items);
            return new PaginatedResponse<QuestResponseDto>(dtos, totalCount, query.PageNumber, query.PageSize);
        }

        public async Task<PaginatedResponse<QuestResponseDto>> GetPublicQuestsAsync(QuestQueryDto query, int? userId = null)
        {
            var (items, totalCount) = await _questRepository.GetPublicQuestsAsync(query.CampaignId, query.IsStandalone, query.IsTierUp, query.PageNumber, query.PageSize, userId, query.IsCompleted);
            var dtos = await MapToResponseDtosAsync(items);
            return new PaginatedResponse<QuestResponseDto>(dtos, totalCount, query.PageNumber, query.PageSize);
        }

        public async Task<PaginatedResponse<UserQuestTaskGroupedDto>> GetUserQuestTasksByQuestAsync(UserQuestTaskQueryDto query)
        {
            var (items, totalCount) = await _userQuestRepository.GetUserQuestTasksByQuestAsync(query);
            var dtos = items.Select(MapToUserQuestTaskGroupedDto).ToList();
            return new PaginatedResponse<UserQuestTaskGroupedDto>(dtos, totalCount, query.PageNumber, query.PageSize);
        }

        public async Task<List<UserQuestProgressDto>> GetCampaignQuestProgressAsync(int userId, int campaignId)
        {
            var userQuests = await _userQuestRepository.GetByUserAndCampaignAsync(userId, campaignId);
            return userQuests.Select(MapToProgressDto).ToList();
        }

        public async Task<UserQuestProgressDto> EnrollInQuestAsync(int userId, int questId)
        {
            var quest = await _questRepository.GetByIdAsync(questId);
            if (quest == null)
                throw new DomainExceptions("Không tìm thấy quest");

            if (!quest.IsActive)
                throw new DomainExceptions("Quest hiện không khả dụng");

            if (!quest.RequiresEnrollment)
                throw new DomainExceptions("Quest này không cho phép đăng ký thủ công.");

            // Check if the user has any previous record for this quest
            var existing = await _userQuestRepository.GetByUserAndQuestAnyStatusAsync(userId, questId);

            if (existing != null)
            {
                if (existing.Status == "IN_PROGRESS")
                    throw new DomainExceptions("Bạn đã tham gia quest này rồi");

                if (existing.Status == "COMPLETED")
                    throw new DomainExceptions("Bạn đã hoàn thành quest này rồi");

                if (existing.Status == "EXPIRED")
                    throw new DomainExceptions("Quest này đã hết hạn với bạn");

                // STOPPED: allow re-enrollment (resume)
                if (existing.Status == "STOPPED")
                {
                    // For standalone quests, enforce the 1-active limit before resuming
                    if (quest.IsStandalone)
                    {
                        var activeStandalone = await _userQuestRepository.GetActiveStandaloneQuestAsync(userId);
                        if (activeStandalone != null && activeStandalone.UserQuestId != existing.UserQuestId)
                            throw new DomainExceptions("Bạn đang có một quest độc lập đang hoạt động. Hãy dừng nó trước khi bắt đầu quest khác.");
                    }

                    existing.Status = "IN_PROGRESS";
                    await _userQuestRepository.UpdateUserQuestAsync(existing);

                    var resumed = await _userQuestRepository.GetByIdAsync(existing.UserQuestId);
                    return MapToProgressDto(resumed!);
                }
            }

            // New enrollment — enforce standalone limit
            if (quest.IsStandalone)
            {
                var activeStandalone = await _userQuestRepository.GetActiveStandaloneQuestAsync(userId);
                if (activeStandalone != null)
                    throw new DomainExceptions("Bạn đang có một quest độc lập đang hoạt động. Hãy dừng nó trước khi bắt đầu quest khác.");
            }

            var userQuest = new UserQuest
            {
                UserId = userId,
                QuestId = questId,
                Status = "IN_PROGRESS",
                StartedAt = DateTime.UtcNow
            };

            var created = await _userQuestRepository.CreateAsync(userQuest);

            var userQuestTasks = quest.QuestTasks.Select(qt => new UserQuestTask
            {
                UserQuestId = created.UserQuestId,
                QuestTaskId = qt.QuestTaskId,
                CurrentValue = 0,
                IsCompleted = false,
                RewardClaimed = false
            }).ToList();

            await _userQuestRepository.AddUserQuestTasksAsync(userQuestTasks);

            var loaded = await _userQuestRepository.GetByIdAsync(created.UserQuestId);
            return MapToProgressDto(loaded!);
        }

        public async Task<UserQuestProgressDto> StopQuestAsync(int userId, int questId)
        {
            var existing = await _userQuestRepository.GetByUserAndQuestAnyStatusAsync(userId, questId);
            if (existing == null)
                throw new DomainExceptions("Bạn chưa tham gia quest này");

            if (existing.Status != "IN_PROGRESS")
                throw new DomainExceptions($"Không thể dừng quest — trạng thái hiện tại là {existing.Status}");

            var quest = await _questRepository.GetByIdAsync(questId);
            if (quest != null && !quest.IsStandalone)
                throw new DomainExceptions("Chỉ quest độc lập mới có thể dừng thủ công");

            existing.Status = "STOPPED";
            await _userQuestRepository.UpdateUserQuestAsync(existing);

            var loaded = await _userQuestRepository.GetByIdAsync(existing.UserQuestId);
            return MapToProgressDto(loaded!);
        }

        public async Task<PaginatedResponse<UserQuestProgressDto>> GetMyQuestsAsync(int userId, string? status, bool? isTierUp = null, int pageNumber = 1, int pageSize = 10)
        {
            var (items, totalCount) = await _userQuestRepository.GetByUserIdAsync(userId, status, isTierUp, pageNumber, pageSize);
            var dtos = items.Select(MapToProgressDto).ToList();
            return new PaginatedResponse<UserQuestProgressDto>(dtos, totalCount, pageNumber, pageSize);
        }

        public async Task<QuestResponseDto> UpdateQuestImageAsync(int questId, string imageUrl)
        {
            var quest = await _questRepository.GetByIdAsync(questId)
                ?? throw new DomainExceptions($"Không tìm thấy quest với ID {questId}");

            quest.ImageUrl = imageUrl;
            await _questRepository.UpdateAsync(quest);
            return await MapToResponseDtoAsync(quest);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void ValidateStandaloneCampaignConsistency(bool isStandalone, int? campaignId)
        {
            if (isStandalone && campaignId.HasValue)
                throw new DomainExceptions("Quest độc lập không thể thuộc về một chiến dịch.");

            if (!isStandalone && !campaignId.HasValue)
                throw new DomainExceptions("Quest chiến dịch phải chỉ định một chiến dịch (CampaignId là bắt buộc).");
        }

        private async Task ValidateTaskRewardAsync(QuestRewardType rewardType, int rewardValue)
        {
            switch (rewardType)
            {
                case QuestRewardType.BADGE:
                    var badge = await _badgeRepository.GetById(rewardValue);
                    if (badge == null)
                        throw new DomainExceptions($"Không tìm thấy huy hiệu với ID {rewardValue}");
                    if (!badge.IsActive)
                        throw new DomainExceptions($"Không thể sử dụng huy hiệu với ID {rewardValue} đã bị vô hiệu hóa.");
                    break;
                case QuestRewardType.VOUCHER:
                    var voucher = await _voucherRepository.GetByIdAsync(rewardValue);
                    if (voucher == null)
                        throw new DomainExceptions($"Không tìm thấy voucher với ID {rewardValue}");
                    break;
                case QuestRewardType.POINTS:
                    break;
            }
        }

        private async Task RemoveVouchersFromTasksAsync(IEnumerable<QuestTask> tasks)
        {
            if (tasks == null) return;

            foreach (var task in tasks)
            {
                if (task.QuestTaskRewards == null) continue;

                foreach (var reward in task.QuestTaskRewards)
                {
                    if (reward.RewardType == QuestRewardType.VOUCHER)
                    {
                        var voucher = await _voucherRepository.GetByIdAsync(reward.RewardValue);
                        if (voucher != null)
                        {
                            var isMarketplaceVoucher = voucher.VendorCampaignId == null && voucher.RedeemPoint > 0;
                            if (!isMarketplaceVoucher)
                            {
                                var hasUsers = await _userVoucherRepository.HasUsersClaimedVoucherAsync(voucher.VoucherId);
                                if (!hasUsers)
                                {
                                    await _voucherRepository.DeleteAsync(voucher.VoucherId);
                                }
                                else
                                {
                                    voucher.IsActive = false;
                                    await _voucherRepository.UpdateAsync(voucher);
                                }
                            }
                        }
                    }
                }
            }
        }

        private async Task<QuestResponseDto> MapToResponseDtoAsync(Quest quest)
        {
            var counts = await _questRepository.GetUserQuestCountsByQuestIdsAsync(new List<int> { quest.QuestId });
            counts.TryGetValue(quest.QuestId, out var userQuestCount);
            return MapToResponseDto(quest, userQuestCount);
        }

        private async Task<List<QuestResponseDto>> MapToResponseDtosAsync(IEnumerable<Quest> quests)
        {
            var questList = quests.ToList();
            var questIds = questList.Select(quest => quest.QuestId).ToList();
            var counts = await _questRepository.GetUserQuestCountsByQuestIdsAsync(questIds);

            return questList
                .Select(quest =>
                {
                    counts.TryGetValue(quest.QuestId, out var userQuestCount);
                    return MapToResponseDto(quest, userQuestCount);
                })
                .ToList();
        }

        private static QuestResponseDto MapToResponseDto(Quest quest, int userQuestCount = 0)
        {
            return new QuestResponseDto
            {
                QuestId = quest.QuestId,
                Title = quest.Title,
                Description = quest.Description,
                ImageUrl = quest.ImageUrl,
                IsActive = quest.IsActive,
                IsStandalone = quest.IsStandalone,
                RequiresEnrollment = quest.RequiresEnrollment,
                CampaignId = quest.CampaignId,
                CreatedAt = quest.CreatedAt,
                UpdatedAt = quest.UpdatedAt,
                TaskCount = quest.QuestTasks.Count,
                UserQuestCount = userQuestCount,
                Tasks = quest.QuestTasks.Select(t => new QuestTaskResponseDto
                {
                    QuestTaskId = t.QuestTaskId,
                    QuestId = quest.QuestId,
                    Type = t.Type,
                    TargetValue = t.TargetValue,
                    Description = t.Description,
                    Rewards = t.QuestTaskRewards.Select(r => new QuestTaskRewardDto
                    {
                        QuestTaskRewardId = r.QuestTaskRewardId,
                        RewardType = r.RewardType,
                        RewardValue = r.RewardValue,
                        Quantity = r.Quantity
                    }).ToList()
                }).ToList()
            };
        }


        private static UserProfileDto MapToUserProfileDto(User user)
        {
            return new UserProfileDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status,
                Role = user.Role.ToString(),
                Point = user.Point,
                XP = user.XP,
                TierId = user.TierId,
                TierName = user.Tier?.Name ?? GetTierNameHardcoded(user.TierId),
                NextTierXP = null
            };
        }

        private static UserQuestTaskGroupedDto MapToUserQuestTaskGroupedDto(UserQuest uq)
        {
            return new UserQuestTaskGroupedDto
            {
                UserQuestId = uq.UserQuestId,
                UserId = uq.UserId,
                User = MapToUserProfileDto(uq.User),
                QuestId = uq.QuestId,
                QuestTitle = uq.Quest.Title,
                Status = uq.Status,
                Tasks = uq.UserQuestTasks.Select(uqt => new UserQuestTaskProgressDto
                {
                    UserQuestTaskId = uqt.UserQuestTaskId,
                    QuestTaskId = uqt.QuestTaskId,
                    Type = uqt.QuestTask.Type,
                    TargetValue = uqt.QuestTask.TargetValue,
                    Description = uqt.QuestTask.Description,
                    Rewards = uqt.QuestTask.QuestTaskRewards.Select(r => new QuestTaskRewardDto
                    {
                        QuestTaskRewardId = r.QuestTaskRewardId,
                        RewardType = r.RewardType,
                        RewardValue = r.RewardValue,
                        Quantity = r.Quantity
                    }).ToList(),
                    CurrentValue = uqt.CurrentValue,
                    IsCompleted = uqt.IsCompleted,
                    CompletedAt = uqt.CompletedAt,
                    RewardClaimed = uqt.RewardClaimed
                }).ToList()
            };
        }

        private static string GetTierNameHardcoded(int? tierId)
        {
            return tierId switch
            {
                1 => "Warning",
                2 => "Silver",
                3 => "Gold",
                4 => "Diamond",
                _ => "Unknown"
            };
        }
        private static UserQuestProgressDto MapToProgressDto(UserQuest uq)
        {
            var tasks = uq.UserQuestTasks.Select(uqt => new UserQuestTaskProgressDto
            {
                UserQuestTaskId = uqt.UserQuestTaskId,
                QuestTaskId = uqt.QuestTaskId,
                Type = uqt.QuestTask.Type,
                TargetValue = uqt.QuestTask.TargetValue,
                Description = uqt.QuestTask.Description,
                Rewards = uqt.QuestTask.QuestTaskRewards.Select(r => new QuestTaskRewardDto
                {
                    QuestTaskRewardId = r.QuestTaskRewardId,
                    RewardType = r.RewardType,
                    RewardValue = r.RewardValue,
                    Quantity = r.Quantity
                }).ToList(),
                CurrentValue = uqt.CurrentValue,
                IsCompleted = uqt.IsCompleted,
                CompletedAt = uqt.CompletedAt,
                RewardClaimed = uqt.RewardClaimed
            }).ToList();

            return new UserQuestProgressDto
            {
                UserQuestId = uq.UserQuestId,
                QuestId = uq.QuestId,
                Title = uq.Quest.Title,
                Description = uq.Quest.Description,
                ImageUrl = uq.Quest.ImageUrl,
                IsStandalone = uq.Quest.IsStandalone,
                Status = uq.Status,
                StartedAt = uq.StartedAt,
                CompletedAt = uq.CompletedAt,
                CampaignId = uq.Quest.CampaignId,
                TotalTasks = tasks.Count,
                CompletedTasks = tasks.Count(t => t.IsCompleted),
                Tasks = tasks
            };
        }
    }
}
