using BO.Common;
using BO.DTO.Quest;
using BO.Entities;
using BO.Enums;
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
                throw new Exception("At least one task is required");

            if (dto.CampaignId.HasValue)
            {
                var campaign = await _campaignRepository.GetByIdAsync(dto.CampaignId.Value);
                if (campaign == null)
                    throw new Exception("Campaign not found");
            }

            // Validate each task's reward references
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
                throw new Exception($"Quest with ID {questId} not found");

            if (!string.IsNullOrEmpty(dto.Title))
                quest.Title = dto.Title;
            if (dto.Description != null)
                quest.Description = dto.Description;
            if (dto.ImageUrl != null)
                quest.ImageUrl = dto.ImageUrl;
            if (dto.IsActive.HasValue)
                quest.IsActive = dto.IsActive.Value;
            if (dto.CampaignId.HasValue)
            {
                var campaign = await _campaignRepository.GetByIdAsync(dto.CampaignId.Value);
                if (campaign == null)
                    throw new Exception("Campaign not found");
                quest.CampaignId = dto.CampaignId.Value;
            }

            if (dto.Tasks != null)
            {
                foreach (var taskDto in dto.Tasks)
                {
                    await ValidateTaskRewardAsync(taskDto.RewardType, taskDto.RewardValue);
                }

                // Remove old tasks and add new ones
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
                throw new Exception("Cannot delete quest while users are enrolled");

            return await _questRepository.DeleteAsync(questId);
        }

        public async Task<QuestResponseDto?> GetQuestByIdAsync(int questId)
        {
            var quest = await _questRepository.GetByIdAsync(questId);
            return quest == null ? null : MapToResponseDto(quest);
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
                throw new Exception("Quest not found");

            if (!quest.IsActive)
                throw new Exception("Quest is not available");

            var existing = await _userQuestRepository.GetByUserAndQuestAsync(userId, questId);
            if (existing != null)
                throw new Exception("You are already enrolled in this quest");

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

            // Reload with tasks
            var loaded = await _userQuestRepository.GetByIdAsync(created.UserQuestId);
            return MapToProgressDto(loaded!);
        }

        public async Task<List<UserQuestProgressDto>> GetMyQuestsAsync(int userId, string? status)
        {
            var userQuests = await _userQuestRepository.GetByUserIdAsync(userId, status);
            return userQuests.Select(MapToProgressDto).ToList();
        }

        private async Task ValidateTaskRewardAsync(QuestRewardType rewardType, int rewardValue)
        {
            switch (rewardType)
            {
                case QuestRewardType.BADGE:
                    var badgeExists = await _badgeRepository.Exists(rewardValue);
                    if (!badgeExists)
                        throw new Exception($"Badge with ID {rewardValue} not found");
                    break;
                case QuestRewardType.VOUCHER:
                    var voucher = await _voucherRepository.GetByIdAsync(rewardValue);
                    if (voucher == null)
                        throw new Exception($"Voucher with ID {rewardValue} not found");
                    break;
                case QuestRewardType.POINTS:
                    // No external reference to validate
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
