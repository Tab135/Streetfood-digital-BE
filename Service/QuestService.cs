using BO.Common;
using BO.DTO.Quest;
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

        public QuestService(
            IQuestRepository questRepository,
            IUserQuestRepository userQuestRepository,
            ICampaignRepository campaignRepository,
            IBadgeRepository badgeRepository,
            IVoucherRepository voucherRepository)
        {
            _questRepository = questRepository;
            _userQuestRepository = userQuestRepository;
            _campaignRepository = campaignRepository;
            _badgeRepository = badgeRepository;
            _voucherRepository = voucherRepository;
        }

        public async Task<QuestResponseDto> CreateQuestAsync(CreateQuestDto dto)
        {
            if (dto.Tasks == null || dto.Tasks.Count == 0)
                throw new DomainExceptions("Cần có ít nhất một nhiệm vụ");

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

            foreach (var taskDto in dto.Tasks)
            {
                await ValidateTaskRewardAsync(taskDto.RewardType, taskDto.RewardValue);
            }

            var quest = new Quest
            {
                Title = dto.Title,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                IsActive = dto.IsActive,
                IsStandalone = dto.IsStandalone,
                CampaignId = dto.CampaignId,
                QuestTasks = dto.Tasks.Select(t => new QuestTask
                {
                    Type = t.Type,
                    TargetValue = t.TargetValue,
                    Description = t.Description,
                    RewardType = t.RewardType,
                    RewardValue = t.RewardValue
                }).ToList()
            };

            var created = await _questRepository.CreateAsync(quest);
            return MapToResponseDto(created);
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
                quest.IsActive = dto.IsActive.Value;
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

            if (dto.Tasks != null)
            {
                foreach (var taskDto in dto.Tasks)
                {
                    await ValidateTaskRewardAsync(taskDto.RewardType, taskDto.RewardValue);
                }

                await _questRepository.RemoveTasksAsync(quest.QuestTasks.ToList());
                var newTasks = dto.Tasks.Select(t => new QuestTask
                {
                    QuestId = questId,
                    Type = t.Type,
                    TargetValue = t.TargetValue,
                    Description = t.Description,
                    RewardType = t.RewardType,
                    RewardValue = t.RewardValue
                }).ToList();
                await _questRepository.AddTasksAsync(newTasks);
                quest.QuestTasks = newTasks;
            }

            await _questRepository.UpdateAsync(quest);
            return MapToResponseDto(quest);
        }

        public async Task<bool> DeleteQuestAsync(int questId)
        {
            var hasUsers = await _questRepository.HasEnrolledUsersAsync(questId);
            if (hasUsers)
                throw new DomainExceptions("Không thể xóa quest khi vẫn còn người dùng đang tham gia");

            return await _questRepository.DeleteAsync(questId);
        }

        public async Task<QuestResponseDto?> GetQuestByIdAsync(int questId)
        {
            var quest = await _questRepository.GetByIdAsync(questId);
            return quest == null ? null : MapToResponseDto(quest);
        }

        public async Task<QuestTaskResponseDto?> GetQuestTaskByIdAsync(int questTaskId)
        {
            var task = await _questRepository.GetTaskByIdAsync(questTaskId);
            if (task == null) return null;
            return new QuestTaskResponseDto
            {
                QuestTaskId = task.QuestTaskId,
                Type = task.Type,
                TargetValue = task.TargetValue,
                Description = task.Description,
                RewardType = task.RewardType,
                RewardValue = task.RewardValue
            };
        }

        public async Task<PaginatedResponse<QuestResponseDto>> GetQuestsAsync(QuestQueryDto query)
        {
            var (items, totalCount) = await _questRepository.GetQuestsAsync(
                query.IsActive, query.CampaignId, query.PageNumber, query.PageSize);

            var dtos = items.Select(MapToResponseDto).ToList();
            return new PaginatedResponse<QuestResponseDto>(dtos, totalCount, query.PageNumber, query.PageSize);
        }

        public async Task<PaginatedResponse<QuestResponseDto>> GetPublicQuestsAsync(QuestQueryDto query)
        {
            var (items, totalCount) = await _questRepository.GetPublicQuestsAsync(query.CampaignId, query.PageNumber, query.PageSize);
            var dtos = items.Select(MapToResponseDto).ToList();
            return new PaginatedResponse<QuestResponseDto>(dtos, totalCount, query.PageNumber, query.PageSize);
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

        public async Task<List<UserQuestProgressDto>> GetMyQuestsAsync(int userId, string? status)
        {
            var userQuests = await _userQuestRepository.GetByUserIdAsync(userId, status);
            return userQuests.Select(MapToProgressDto).ToList();
        }

        public async Task<QuestResponseDto> UpdateQuestImageAsync(int questId, string imageUrl)
        {
            var quest = await _questRepository.GetByIdAsync(questId)
                ?? throw new DomainExceptions($"Không tìm thấy quest với ID {questId}");

            quest.ImageUrl = imageUrl;
            await _questRepository.UpdateAsync(quest);
            return MapToResponseDto(quest);
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
                    var badgeExists = await _badgeRepository.Exists(rewardValue);
                    if (!badgeExists)
                        throw new DomainExceptions($"Không tìm thấy huy hiệu với ID {rewardValue}");
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

        private static QuestResponseDto MapToResponseDto(Quest quest)
        {
            return new QuestResponseDto
            {
                QuestId = quest.QuestId,
                Title = quest.Title,
                Description = quest.Description,
                ImageUrl = quest.ImageUrl,
                IsActive = quest.IsActive,
                IsStandalone = quest.IsStandalone,
                CampaignId = quest.CampaignId,
                CreatedAt = quest.CreatedAt,
                UpdatedAt = quest.UpdatedAt,
                TaskCount = quest.QuestTasks.Count,
                Tasks = quest.QuestTasks.Select(t => new QuestTaskResponseDto
                {
                    QuestTaskId = t.QuestTaskId,
                    Type = t.Type,
                    TargetValue = t.TargetValue,
                    Description = t.Description,
                    RewardType = t.RewardType,
                    RewardValue = t.RewardValue
                }).ToList()
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
                RewardType = uqt.QuestTask.RewardType,
                RewardValue = uqt.QuestTask.RewardValue,
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
