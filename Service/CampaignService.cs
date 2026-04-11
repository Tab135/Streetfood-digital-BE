using BO.DTO.Campaigns;
using BO.Entities;
using BO.Exceptions;
using Hangfire;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service
{
    public class CampaignService : ICampaignService
    {
        private readonly ICampaignRepository _campaignRepo;
        private readonly IBranchCampaignRepository _branchCampaignRepo;
        private readonly IBranchRepository _branchRepo;
        private readonly IVendorRepository _vendorRepo;
        private readonly Service.PaymentsService.IPaymentService _paymentService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public CampaignService(
            ICampaignRepository campaignRepo,
            IBranchCampaignRepository branchCampaignRepo,
            IBranchRepository branchRepo,
            IVendorRepository vendorRepo,
            Service.PaymentsService.IPaymentService paymentService,
            IBackgroundJobClient backgroundJobClient)
        {
            _campaignRepo = campaignRepo;
            _branchCampaignRepo = branchCampaignRepo;
            _branchRepo = branchRepo;
            _vendorRepo = vendorRepo;
            _paymentService = paymentService;
            _backgroundJobClient = backgroundJobClient;
        }

        private static DateTimeOffset ResolveHangfireRunAt(DateTime targetUtc)
        {
            var now = DateTimeOffset.UtcNow;
            var normalizedTargetUtc = targetUtc.Kind == DateTimeKind.Utc
                ? targetUtc
                : DateTime.SpecifyKind(targetUtc, DateTimeKind.Utc);

            var target = new DateTimeOffset(normalizedTargetUtc);
            return target > now ? target : now.AddSeconds(5);
        }

        private void ScheduleCampaignStatusJobs(int campaignId, DateTime startDate, DateTime endDate)
        {
            _backgroundJobClient.Schedule<ICampaignStatusJob>(
                job => job.ActivateCampaignAsync(campaignId),
                ResolveHangfireRunAt(startDate));

            _backgroundJobClient.Schedule<ICampaignStatusJob>(
                job => job.DeactivateCampaignAsync(campaignId),
                ResolveHangfireRunAt(endDate));
        }

        public async Task<CampaignResponseDto> CreateVendorCampaignAsync(int userId, CreateVendorCampaignDto dto)
        {
            var vendor = await _vendorRepo.GetByUserIdAsync(userId);
            if (vendor == null)
                throw new DomainExceptions("Không tìm thấy Vendor của người dùng này.");

            var targetBranchIds = await ResolveTargetBranchIdsForVendorCampaignAsync(vendor.VendorId, dto.BranchIds);
            int? createdByBranchId = targetBranchIds.Count == 1 ? targetBranchIds[0] : null;

            bool isCampaignActive = dto.IsActive;
            if (dto.StartDate > DateTime.UtcNow)
            {
                isCampaignActive = false;
            }

            var campaign = new Campaign
            {
                Name = dto.Name,
                Description = dto.Description,
                TargetSegment = dto.TargetSegment,
                RegistrationStartDate = null,
                RegistrationEndDate = null,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = isCampaignActive,
                CreatedByVendorId = vendor.VendorId,
                CreatedByBranchId = createdByBranchId
            };
            await _campaignRepo.CreateAsync(campaign);
            foreach (var branchId in targetBranchIds)
            {
                var existing = await _branchCampaignRepo.GetByBranchAndCampaignAsync(branchId, campaign.CampaignId);
                if (existing != null)
                    continue;

                await _branchCampaignRepo.CreateAsync(new BO.Entities.BranchCampaign
                {
                    BranchId = branchId,
                    CampaignId = campaign.CampaignId,
                    IsActive = true
                });
            }

            ScheduleCampaignStatusJobs(campaign.CampaignId, campaign.StartDate, campaign.EndDate);

            return await GetCampaignByIdAsync(campaign.CampaignId);
        }

        public async Task<BO.Common.PaginatedResponse<BO.DTO.Campaigns.CampaignBranchResponseDto>> GetBranchesInAnyVendorCampaignAsync(int pageNumber, int pageSize, double? userLat, double? userLng, double? maxDistance = 5.0)
        {
            var (items, totalCount) = await _campaignRepo.GetBranchesInAnyVendorCampaignPaginatedAsync(pageNumber, pageSize, userLat, userLng, maxDistance);
            return new BO.Common.PaginatedResponse<BO.DTO.Campaigns.CampaignBranchResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<BO.Common.PaginatedResponse<BO.DTO.Campaigns.CampaignBranchResponseDto>> GetSystemCampaignBranchesAsync(int campaignId, int pageNumber, int pageSize, double? userLat, double? userLng)
        {
            var campaign = await _campaignRepo.GetByIdAsync(campaignId);
            if (campaign == null || (campaign.CreatedByVendorId != null || campaign.CreatedByBranchId != null))
            {
                throw new DomainExceptions("Chiến dịch hệ thống không tồn tại hoặc đây không phải chiến dịch hệ thống.");
            }

            var (items, totalCount) = await _campaignRepo.GetCampaignBranchesPaginatedAsync(campaignId, pageNumber, pageSize, userLat, userLng);
            return new BO.Common.PaginatedResponse<BO.DTO.Campaigns.CampaignBranchResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<BO.Common.PaginatedResponse<BO.DTO.Campaigns.CampaignBranchResponseDto>> GetVendorCampaignBranchesByCampaignIdAsync(int campaignId, int pageNumber, int pageSize, double? userLat, double? userLng)
        {
            var campaign = await _campaignRepo.GetByIdAsync(campaignId);
            if (campaign == null || campaign.CreatedByVendorId == null)
            {
                throw new DomainExceptions("Chiến dịch vendor không tồn tại hoặc đây là chiến dịch hệ thống.");
            }

            var (items, totalCount) = await _campaignRepo.GetCampaignBranchesPaginatedAsync(campaignId, pageNumber, pageSize, userLat, userLng, includeInactiveBranches: true);
            return new BO.Common.PaginatedResponse<BO.DTO.Campaigns.CampaignBranchResponseDto>(items, totalCount, pageNumber, pageSize);
        }


        public async Task<VendorCampaignBranchesResponseDto> AddBranchesToVendorCampaignAsync(int userId, int campaignId, List<int> branchIds)
        {
            var vendor = await _vendorRepo.GetByUserIdAsync(userId);
            if (vendor == null)
                throw new DomainExceptions("Không tìm thấy Vendor của người dùng này.");

            var campaign = await _campaignRepo.GetByIdAsync(campaignId);
            if (campaign == null || campaign.CreatedByVendorId != vendor.VendorId)
                throw new DomainExceptions("Chiến dịch không tồn tại hoặc không phải campaign của vendor này.");

            var ids = (branchIds ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0)
                throw new DomainExceptions("Danh sách BranchIds không hợp lệ hoặc rỗng.");

            var branches = await _branchRepo.GetAllByVendorIdAsync(vendor.VendorId);
            var branchMap = branches.ToDictionary(b => b.BranchId);

            foreach (var bid in ids)
            {
                if (!branchMap.TryGetValue(bid, out var b))
                    throw new DomainExceptions($"Chi nhánh {bid} không thuộc vendor hoặc không tồn tại.");
                if (!IsEligibleForVendorCampaignBranch(b))
                    throw new DomainExceptions($"Chi nhánh {bid} không đủ điều kiện tham gia (tier / subscription).");

                var existing = await _branchCampaignRepo.GetByBranchAndCampaignAsync(bid, campaignId);
                if (existing != null)
                    continue;

                await _branchCampaignRepo.CreateAsync(new BO.Entities.BranchCampaign
                {
                    BranchId = bid,
                    CampaignId = campaignId,
                    IsActive = true
                });
            }

            await ReactivateVendorCampaignAfterBranchesAddedAsync(campaign, vendor.VendorId);
            return await BuildVendorCampaignBranchesResponseAsync(vendor.VendorId, campaignId);
        }

        public async Task<VendorCampaignBranchesResponseDto> RemoveBranchesFromVendorCampaignAsync(int userId, int campaignId, List<int> branchIds)
        {
            var vendor = await _vendorRepo.GetByUserIdAsync(userId);
            if (vendor == null)
                throw new DomainExceptions("Không tìm thấy Vendor của người dùng này.");

            var campaign = await _campaignRepo.GetByIdAsync(campaignId);
            if (campaign == null || campaign.CreatedByVendorId != vendor.VendorId)
                throw new DomainExceptions("Chiến dịch không tồn tại hoặc không phải campaign của vendor này.");

            var ids = (branchIds ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0)
                throw new DomainExceptions("Danh sách BranchIds không hợp lệ hoặc rỗng.");

            foreach (var bid in ids)
            {
                var branch = await _branchRepo.GetByIdAsync(bid);
                if (branch == null || branch.VendorId != vendor.VendorId)
                    throw new DomainExceptions($"Chi nhánh {bid} không thuộc vendor hoặc không tồn tại.");

                await _branchCampaignRepo.DeleteByBranchAndCampaignAsync(bid, campaignId);
            }

            await SyncVendorCampaignAfterParticipationChangeAsync(campaign, vendor.VendorId);
            return await BuildVendorCampaignBranchesResponseAsync(vendor.VendorId, campaignId);
        }

        /// <summary>
        /// After <c>branches/add</c>: turn campaign back on and align all <see cref="BranchCampaign"/> rows to active.
        /// </summary>
        private async Task ReactivateVendorCampaignAfterBranchesAddedAsync(Campaign campaign, int vendorId)
        {
            var remainingAllBranches = await _branchCampaignRepo.CountByCampaignIdAsync(campaign.CampaignId);
            if (remainingAllBranches == 0)
                return;

            await _branchCampaignRepo.SetAllIsActiveForCampaignAsync(campaign.CampaignId, true);

            var ids = await _branchCampaignRepo.GetBranchIdsByCampaignAndVendorAsync(campaign.CampaignId, vendorId);
            int? nextBranchId = ids.Count == 1 ? ids[0] : null;

            bool shouldBeActive = campaign.StartDate <= DateTime.UtcNow;

            if (campaign.CreatedByBranchId == nextBranchId && campaign.IsActive == shouldBeActive)
                return;

            campaign.CreatedByBranchId = nextBranchId;
            campaign.IsActive = shouldBeActive;
            campaign.UpdatedAt = DateTime.UtcNow;
            await _campaignRepo.UpdateAsync(campaign);
        }

        /// <summary>
        /// Keeps <see cref="Campaign.CreatedByBranchId"/> in sync (exactly one branch → set, else null).
        /// If no <see cref="BranchCampaign"/> rows remain for the campaign → <see cref="Campaign.IsActive"/> = false.
        /// </summary>
        private async Task SyncVendorCampaignAfterParticipationChangeAsync(Campaign campaign, int vendorId)
        {
            var remainingAllBranches = await _branchCampaignRepo.CountByCampaignIdAsync(campaign.CampaignId);
            var ids = await _branchCampaignRepo.GetBranchIdsByCampaignAndVendorAsync(campaign.CampaignId, vendorId);

            int? nextBranchId;
            bool nextActive = campaign.IsActive;

            if (remainingAllBranches == 0)
            {
                nextBranchId = null;
                nextActive = false;
            }
            else
            {
                nextBranchId = ids.Count == 1 ? ids[0] : null;
                nextActive = campaign.StartDate <= DateTime.UtcNow;
            }

            if (campaign.CreatedByBranchId == nextBranchId && campaign.IsActive == nextActive)
                return;

            campaign.CreatedByBranchId = nextBranchId;
            campaign.IsActive = nextActive;
            campaign.UpdatedAt = DateTime.UtcNow;
            await _campaignRepo.UpdateAsync(campaign);
        }

        private async Task<VendorCampaignBranchesResponseDto> BuildVendorCampaignBranchesResponseAsync(int vendorId, int campaignId)
        {
            var ids = await _branchCampaignRepo.GetBranchIdsByCampaignAndVendorAsync(campaignId, vendorId);
            return new VendorCampaignBranchesResponseDto
            {
                CampaignId = campaignId,
                BranchIds = ids
            };
        }

        /// <summary>
        /// Null or omitted: all eligible branches. Non-empty list: only those IDs. Empty JSON array is rejected (use null for "all").
        /// </summary>
        private async Task<List<int>> ResolveTargetBranchIdsForVendorCampaignAsync(int vendorId, List<int>? requestedBranchIds)
        {
            var branches = await _branchRepo.GetAllByVendorIdAsync(vendorId);

            if (requestedBranchIds != null && requestedBranchIds.Count > 0)
            {
                var distinct = requestedBranchIds.Where(id => id > 0).Distinct().ToList();
                if (distinct.Count == 0)
                    throw new DomainExceptions("Danh sách BranchIds không hợp lệ.");

                var result = new List<int>();
                foreach (var bid in distinct)
                {
                    var b = branches.FirstOrDefault(x => x.BranchId == bid);
                    if (b == null)
                        throw new DomainExceptions($"Chi nhánh {bid} không thuộc vendor hoặc không tồn tại.");
                    if (!IsEligibleForVendorCampaignBranch(b))
                        throw new DomainExceptions($"Chi nhánh {bid} không đủ điều kiện tham gia (tier / subscription).");
                    result.Add(bid);
                }

                return result;
            }

            if (requestedBranchIds != null && requestedBranchIds.Count == 0)
                throw new DomainExceptions("BranchIds rỗng: không gửi field BranchIds (hoặc null) để áp dụng tất cả chi nhánh đủ điều kiện, hoặc gửi danh sách BranchId cụ thể.");

            var allEligible = new List<int>();
            foreach (var branch in branches)
            {
                if (IsEligibleForVendorCampaignBranch(branch))
                    allEligible.Add(branch.BranchId);
            }

            return allEligible;
        }

        private static bool IsEligibleForVendorCampaignBranch(Branch branch)
        {
            return branch.Tier != null && branch.Tier.Weight >= 1 && branch.IsSubscribed;
        }

        public async Task<SystemCampaignDetailDto> GetSystemCampaignDetailWithJoinableBranchesAsync(int userId, int campaignId)
        {
            var campaign = await _campaignRepo.GetByIdAsync(campaignId);
            if (campaign == null || campaign.CreatedByBranchId != null || campaign.CreatedByVendorId != null)
                throw new DomainExceptions("Chiến dịch không phải là System Campaign.");

            var vendor = await _vendorRepo.GetByUserIdAsync(userId);
            if (vendor == null) throw new DomainExceptions("Không tìm thấy Vendor của người dùng này.");

            var branches = await _branchRepo.GetAllByVendorIdAsync(vendor.VendorId);
            var eligibleBranchIds = new List<int>();
            foreach (var branch in branches)
            {
                if (!(branch.Tier != null && branch.Tier.Weight >= 1 && branch.IsSubscribed))
                    continue;

                var branchCampaign = await _branchCampaignRepo.GetByBranchAndCampaignAsync(branch.BranchId, campaignId);
                if (branchCampaign != null && branchCampaign.IsActive)
                    continue;

                eligibleBranchIds.Add(branch.BranchId);
            }

            return new SystemCampaignDetailDto
            {
                CampaignId = campaign.CampaignId,
                Name = campaign.Name,
                Description = campaign.Description,
                TargetSegment = campaign.TargetSegment,
                RegistrationStartDate = campaign.RegistrationStartDate,
                RegistrationEndDate = campaign.RegistrationEndDate,
                StartDate = campaign.StartDate,
                EndDate = campaign.EndDate,
                IsActive = campaign.IsActive,
                ImageUrl = campaign.ImageUrl,
                JoinableBranch = eligibleBranchIds
            };
        }

        public async Task<VendorJoinSystemCampaignResultDto> VendorJoinSystemCampaignAsync(int userId, int campaignId)
        {
            var campaign = await _campaignRepo.GetByIdAsync(campaignId);
            if (campaign == null || campaign.CreatedByBranchId != null || campaign.CreatedByVendorId != null)
                throw new DomainExceptions("Chiến dịch không phải là System Campaign.");

            var vendor = await _vendorRepo.GetByUserIdAsync(userId);
            if (vendor == null) throw new DomainExceptions("Không tìm thấy Vendor của người dùng này.");

            var branches = await _branchRepo.GetAllByVendorIdAsync(vendor.VendorId);
            var result = new VendorJoinSystemCampaignResultDto();

            var pendingBranchCampaignIds = new List<int>();
            var pendingBranchDtos = new List<VendorJoinSystemCampaignBranchDto>();

            foreach (var branch in branches)
            {
                // Fail condition: branch doesn't meet tier/subscription requirements -> skip entirely
                if (!(branch.Tier != null && branch.Tier.Weight >= 1 && branch.IsSubscribed))
                    continue;

                var branchCampaign = await _branchCampaignRepo.GetByBranchAndCampaignAsync(branch.BranchId, campaignId);
                if (branchCampaign != null && branchCampaign.IsActive)
                {
                    result.Branches.Add(new VendorJoinSystemCampaignBranchDto
                    {
                        BranchId = branch.BranchId,
                        Status = "ALREADY_JOINED"
                    });
                    continue;
                }

                // Pending join: create BranchCampaign if doesn't exist yet
                int branchCampaignId;
                if (branchCampaign == null)
                {
                    var newJoin = new BO.Entities.BranchCampaign
                    {
                        BranchId = branch.BranchId,
                        CampaignId = campaignId,
                        IsActive = false // Not paid yet
                    };
                    var created = await _branchCampaignRepo.CreateAsync(newJoin);
                    branchCampaignId = created.Id;
                }
                else
                {
                    branchCampaignId = branchCampaign.Id;
                }

                pendingBranchCampaignIds.Add(branchCampaignId);
                pendingBranchDtos.Add(new VendorJoinSystemCampaignBranchDto
                {
                    BranchId = branch.BranchId,
                    Status = "PAYMENT_REQUIRED"
                });
            }

            // Nothing to pay: only already joined branches
            if (pendingBranchCampaignIds.Count == 0)
                return result;

            var paymentResult = await _paymentService.CreateVendorSystemCampaignPaymentLink(
                userId,
                campaignId,
                vendor.VendorId,
                pendingBranchCampaignIds);

            foreach (var dto in pendingBranchDtos)
            {
                if (!paymentResult.Success)
                {
                    dto.Status = "PAYMENT_ERROR";
                    continue;
                }

                dto.PaymentUrl = paymentResult.PaymentUrl;
                dto.QrCode = paymentResult.QrCode;
                dto.OrderCode = paymentResult.OrderCode;
                dto.PaymentLinkId = paymentResult.PaymentLinkId;
            }

            result.Branches.AddRange(pendingBranchDtos);
            return result;
        }

        public async Task<VendorJoinSystemCampaignResultDto> VendorJoinSystemCampaignForBranchesAsync(int userId, int campaignId, List<int> branchIds)
        {
            var campaign = await _campaignRepo.GetByIdAsync(campaignId);
            if (campaign == null || campaign.CreatedByBranchId != null || campaign.CreatedByVendorId != null)
                throw new DomainExceptions("Chiến dịch không phải là System Campaign.");

            var vendor = await _vendorRepo.GetByUserIdAsync(userId);
            if (vendor == null) throw new DomainExceptions("Không tìm thấy Vendor của người dùng này.");

            var result = new VendorJoinSystemCampaignResultDto();

            var requested = (branchIds ?? new List<int>())
                .FindAll(id => id > 0);
            // distinct while preserving order
            var distinctRequested = new List<int>();
            var seen = new HashSet<int>();
            foreach (var id in requested)
            {
                if (seen.Add(id)) distinctRequested.Add(id);
            }

            if (distinctRequested.Count == 0)
                throw new DomainExceptions("Danh sách BranchIds không hợp lệ hoặc rỗng.");

            var vendorBranches = await _branchRepo.GetAllByVendorIdAsync(vendor.VendorId);
            var vendorBranchMap = new Dictionary<int, BO.Entities.Branch>();
            foreach (var b in vendorBranches)
            {
                vendorBranchMap[b.BranchId] = b;
            }

            var pendingBranchCampaignIds = new List<int>();
            var pendingBranchDtos = new List<VendorJoinSystemCampaignBranchDto>();

            foreach (var branchId in distinctRequested)
            {
                if (!vendorBranchMap.TryGetValue(branchId, out var branch) || branch == null)
                {
                    result.Branches.Add(new VendorJoinSystemCampaignBranchDto
                    {
                        BranchId = branchId,
                        Status = "NOT_OWNED"
                    });
                    continue;
                }

                if (!(branch.Tier != null && branch.Tier.Weight >= 1 && branch.IsSubscribed))
                {
                    result.Branches.Add(new VendorJoinSystemCampaignBranchDto
                    {
                        BranchId = branchId,
                        Status = "NOT_ELIGIBLE"
                    });
                    continue;
                }

                var branchCampaign = await _branchCampaignRepo.GetByBranchAndCampaignAsync(branchId, campaignId);
                if (branchCampaign != null && branchCampaign.IsActive)
                {
                    result.Branches.Add(new VendorJoinSystemCampaignBranchDto
                    {
                        BranchId = branchId,
                        Status = "ALREADY_JOINED"
                    });
                    continue;
                }

                int branchCampaignId;
                if (branchCampaign == null)
                {
                    var created = await _branchCampaignRepo.CreateAsync(new BO.Entities.BranchCampaign
                    {
                        BranchId = branchId,
                        CampaignId = campaignId,
                        IsActive = false
                    });
                    branchCampaignId = created.Id;
                }
                else
                {
                    branchCampaignId = branchCampaign.Id;
                }

                pendingBranchCampaignIds.Add(branchCampaignId);
                pendingBranchDtos.Add(new VendorJoinSystemCampaignBranchDto
                {
                    BranchId = branchId,
                    Status = "PAYMENT_REQUIRED"
                });
            }

            if (pendingBranchCampaignIds.Count == 0)
                return result;

            var paymentResult = await _paymentService.CreateVendorSystemCampaignPaymentLink(
                userId,
                campaignId,
                vendor.VendorId,
                pendingBranchCampaignIds);

            foreach (var dto in pendingBranchDtos)
            {
                if (!paymentResult.Success)
                {
                    dto.Status = "PAYMENT_ERROR";
                    continue;
                }

                dto.PaymentUrl = paymentResult.PaymentUrl;
                dto.QrCode = paymentResult.QrCode;
                dto.OrderCode = paymentResult.OrderCode;
                dto.PaymentLinkId = paymentResult.PaymentLinkId;
            }

            result.Branches.AddRange(pendingBranchDtos);
            return result;
        }

        public async Task UpdateCampaignImageUrlAsync(int campaignId, string? imageUrl, int userId, string role)
        {
            var campaign = await _campaignRepo.GetByIdAsync(campaignId);
            if (campaign == null) throw new DomainExceptions("Không tìm thấy chiến dịch.");

            if (campaign.CreatedByBranchId == null && campaign.CreatedByVendorId == null)
            {
                if (role != "Admin") throw new DomainExceptions("Chỉ Admin mới có thể thao tác ảnh chiến dịch hệ thống.");
            }
            else
            {
                if (role == "Admin") throw new DomainExceptions("Admin không thể thao tác ảnh campaign của Vendor/Branch.");
                var vendor = await _vendorRepo.GetByUserIdAsync(userId);
                if (vendor == null) throw new DomainExceptions("Không tìm thấy Vendor của người dùng này.");
                bool isOwner = false;
                if (campaign.CreatedByVendorId == vendor.VendorId)
                    isOwner = true;
                else if (campaign.CreatedByBranchId != null)
                {
                    var branch = await _branchRepo.GetByIdAsync(campaign.CreatedByBranchId.Value);
                    if (branch != null && branch.VendorId == vendor.VendorId) isOwner = true;
                }
                if (!isOwner) throw new DomainExceptions("Bạn không có quyền thao tác ảnh cho chiến dịch này.");
            }
            campaign.ImageUrl = imageUrl;
            await _campaignRepo.UpdateAsync(campaign);
        }

        public async Task<string?> GetCampaignImageUrlAsync(int campaignId)
        {
            var campaign = await _campaignRepo.GetByIdAsync(campaignId);
            if (campaign == null) throw new DomainExceptions("Không tìm thấy chiến dịch.");
            return campaign.ImageUrl;
        }

        public async Task<CampaignResponseDto> CreateSystemCampaignAsync(CreateCampaignDto dto)
        {
            var campaign = new Campaign
            {
                Name = dto.Name,
                Description = dto.Description,
                TargetSegment = dto.TargetSegment,
                RegistrationStartDate = dto.RegistrationStartDate,
                RegistrationEndDate = dto.RegistrationEndDate,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive,
                CreatedByBranchId = null
            };
            await _campaignRepo.CreateAsync(campaign);

            ScheduleCampaignStatusJobs(campaign.CampaignId, campaign.StartDate, campaign.EndDate);

            // Schedule quest expiration to fire exactly at EndDate
            _backgroundJobClient.Schedule<IQuestExpirationJob>(
                job => job.ExpireCampaignQuestsAsync(campaign.CampaignId),
                ResolveHangfireRunAt(campaign.EndDate));

            return await GetCampaignByIdAsync(campaign.CampaignId);
        }

        public async Task<int> JoinSystemCampaignAsync(int userId, int branchId, int campaignId)
        {
            var vendor = await _vendorRepo.GetByUserIdAsync(userId);
            if (vendor == null)
                throw new DomainExceptions("Không tìm thấy Vendor của người dùng này.");

            var branch = await _branchRepo.GetByIdAsync(branchId);
            if (branch == null || branch.VendorId != vendor.VendorId)
                throw new DomainExceptions("Không tìm thấy chi nhánh hoặc không có quyền truy cập.");

            var campaign = await _campaignRepo.GetByIdAsync(campaignId);
            if (campaign == null || campaign.CreatedByBranchId != null || campaign.CreatedByVendorId != null)
                throw new DomainExceptions("Chiến dịch hệ thống không tồn tại.");

            var now = DateTime.UtcNow;
            if (campaign.RegistrationStartDate.HasValue && campaign.RegistrationEndDate.HasValue)
            {
                if (now < campaign.RegistrationStartDate || now > campaign.RegistrationEndDate)
                    throw new DomainExceptions("Không nằm trong thời gian tham gia chiến dịch này.");
            }

            // Eligibility check for system campaigns
            if (campaign.CreatedByBranchId == null && campaign.CreatedByVendorId == null)
            {
                if (branch.Tier == null || branch.Tier.Weight < 1)
                {
                    throw new DomainExceptions("Cấp bậc của chi nhánh không đủ điều kiện tham gia chiến dịch system (yêu cầu Tier mặc định trở lên).");
                }

                if (!branch.IsSubscribed)
                {
                    throw new DomainExceptions("Chi nhánh chưa đăng ký subscription (IsSubscribed = false).");
                }
            }

            var existingJoin = await _branchCampaignRepo.GetByBranchAndCampaignAsync(branchId, campaignId);
            if (existingJoin != null)
            {
                if (existingJoin.IsActive)
                    throw new DomainExceptions("Chi nhánh đã tham gia và thanh toán chiến dịch này.");
                
                return existingJoin.Id;
            }

            var joinRequest = new BranchCampaign
            {
                BranchId = branchId,
                CampaignId = campaignId,
                IsActive = false
            };
            await _branchCampaignRepo.CreateAsync(joinRequest);

            return joinRequest.Id;
        }

        public async Task<BO.Common.PaginatedResponse<CampaignResponseDto>> GetSystemCampaignsAsync(CampaignQueryDto query)
        {
            var (items, totalCount) = await _campaignRepo.GetCampaignsAsync(true, null, query.PageNumber, query.PageSize);
            
            var mappedItems = new List<CampaignResponseDto>();
            foreach(var item in items)
            {
                mappedItems.Add(new CampaignResponseDto
                {
                    CampaignId = item.CampaignId,
                    CreatedByBranchId = item.CreatedByBranchId,
                    CreatedByVendorId = item.CreatedByVendorId,
                    Name = item.Name,
                    Description = item.Description,
                    TargetSegment = item.TargetSegment,
                    RegistrationStartDate = item.RegistrationStartDate,
                    RegistrationEndDate = item.RegistrationEndDate,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                    IsActive = item.IsActive,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    ImageUrl = item.ImageUrl
                });
            }

            return new BO.Common.PaginatedResponse<CampaignResponseDto>(
                mappedItems,
                totalCount,
                query.PageNumber,
                query.PageSize
            );
        }

        public async Task<BO.Common.PaginatedResponse<CampaignResponseDto>> GetVendorCampaignsAsync(int userId, string userRole, CampaignQueryDto query)
        {
            int vendorIdToQuery;
            if (userRole == "Admin")
            {
                if (!query.VendorId.HasValue || query.VendorId.Value <= 0)
                    throw new DomainExceptions("Admin cần truyền VendorId (query) để xem danh sách campaign của vendor.");

                var vendorById = await _vendorRepo.GetByIdAsync(query.VendorId.Value);
                if (vendorById == null)
                    throw new DomainExceptions("Vendor không tồn tại.");

                vendorIdToQuery = query.VendorId.Value;
            }
            else
            {
                var vendor = await _vendorRepo.GetByUserIdAsync(userId);
                if (vendor == null) throw new DomainExceptions("Không tìm thấy Vendor của người dùng này.");

                vendorIdToQuery = vendor.VendorId;
            }

            var (items, totalCount) = await _campaignRepo.GetCampaignsAsync(false, vendorIdToQuery, query.PageNumber, query.PageSize);
            
            var mappedItems = new List<CampaignResponseDto>();
            foreach(var item in items)
            {
                mappedItems.Add(new CampaignResponseDto
                {
                    CampaignId = item.CampaignId,
                    CreatedByBranchId = item.CreatedByBranchId,
                    CreatedByVendorId = item.CreatedByVendorId,
                    Name = item.Name,
                    Description = item.Description,
                    TargetSegment = item.TargetSegment,
                    RegistrationStartDate = item.RegistrationStartDate,
                    RegistrationEndDate = item.RegistrationEndDate,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate, 
                    IsActive = item.IsActive,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    ImageUrl = item.ImageUrl
                });
            }

            return new BO.Common.PaginatedResponse<CampaignResponseDto>(
                mappedItems,
                totalCount,
                query.PageNumber,
                query.PageSize
            );
        }

        public async Task<BO.Common.PaginatedResponse<CampaignResponseDto>> GetJoinableSystemCampaignsAsync(CampaignQueryDto query)
        {
            var (items, totalCount) = await _campaignRepo.GetJoinableSystemCampaignsAsync(query.PageNumber, query.PageSize);
            
            var mappedItems = new List<CampaignResponseDto>();
            foreach(var item in items)
            {
                mappedItems.Add(new CampaignResponseDto
                {
                    CampaignId = item.CampaignId,
                    CreatedByBranchId = item.CreatedByBranchId,
                    CreatedByVendorId = item.CreatedByVendorId,
                    Name = item.Name,
                    Description = item.Description,
                    TargetSegment = item.TargetSegment,
                    RegistrationStartDate = item.RegistrationStartDate,
                    RegistrationEndDate = item.RegistrationEndDate,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate, 
                    IsActive = item.IsActive,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    ImageUrl = item.ImageUrl
                });
            }

            return new BO.Common.PaginatedResponse<CampaignResponseDto>(
                mappedItems,
                totalCount,
                query.PageNumber,
                query.PageSize
            );
        }

        public async Task<BO.Common.PaginatedResponse<CampaignResponseDto>> GetPublicCampaignsAsync(CampaignQueryDto query)
        {
            var (items, totalCount) = await _campaignRepo.GetPublicCampaignsAsync(query.IsSystem, query.PageNumber, query.PageSize);
            
            var mappedItems = new List<CampaignResponseDto>();
            foreach(var item in items)
            {
                mappedItems.Add(new CampaignResponseDto
                {
                    CampaignId = item.CampaignId,
                    CreatedByBranchId = item.CreatedByBranchId,
                    CreatedByVendorId = item.CreatedByVendorId,
                    Name = item.Name,
                    Description = item.Description,
                    TargetSegment = item.TargetSegment,
                    RegistrationStartDate = item.RegistrationStartDate,
                    RegistrationEndDate = item.RegistrationEndDate,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate, 
                    IsActive = item.IsActive,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    ImageUrl = item.ImageUrl
                });
            }

            return new BO.Common.PaginatedResponse<CampaignResponseDto>(
                mappedItems,
                totalCount,
                query.PageNumber,
                query.PageSize
            );
        }

        public async Task<CampaignResponseDto> GetCampaignByIdAsync(int id)
        {
            var item = await _campaignRepo.GetByIdAsync(id);
            if (item == null) throw new DomainExceptions("Không tìm thấy chiến dịch.");
            return new CampaignResponseDto
            {
                CampaignId = item.CampaignId,
                CreatedByBranchId = item.CreatedByBranchId,
                CreatedByVendorId = item.CreatedByVendorId,
                Name = item.Name,
                Description = item.Description,
                TargetSegment = item.TargetSegment,
                RegistrationStartDate = item.RegistrationStartDate,
                RegistrationEndDate = item.RegistrationEndDate,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                ImageUrl = item.ImageUrl
            };
        }

        public async Task<CampaignResponseDto> UpdateCampaignAsync(int userId, string role, int campaignId, UpdateCampaignDto dto)
        {
            var campaign = await _campaignRepo.GetByIdAsync(campaignId);
            if (campaign == null) throw new DomainExceptions("Không tìm thấy chiến dịch.");

            var oldStartDate = campaign.StartDate;
            var oldEndDate = campaign.EndDate;
            var oldIsActive = campaign.IsActive;

            if (campaign.CreatedByBranchId == null && campaign.CreatedByVendorId == null)
            {
                if (role != "Admin")
                {
                    throw new DomainExceptions("Chỉ Admin mới có thể cập nhật chiến dịch hệ thống.");
                }
            }
            else
            {
                var vendor = await _vendorRepo.GetByUserIdAsync(userId);
                if (vendor == null) throw new DomainExceptions("Không tìm thấy Vendor của người dùng này.");

                bool isOwner = false;
                if (campaign.CreatedByVendorId == vendor.VendorId)
                {
                    isOwner = true;
                }
                else if (campaign.CreatedByBranchId != null)
                {
                    var branch = await _branchRepo.GetByIdAsync(campaign.CreatedByBranchId.Value);
                    if (branch != null && branch.VendorId == vendor.VendorId)
                    {
                        isOwner = true;
                    }
                }

                if (!isOwner)
                {
                    throw new DomainExceptions("Bạn không có quyền cập nhật chiến dịch này.");
                }
            }

            bool endDateChanged = campaign.EndDate != dto.EndDate;

            campaign.Name = dto.Name;
            campaign.Description = dto.Description;
            campaign.TargetSegment = dto.TargetSegment;
            campaign.RegistrationStartDate = dto.RegistrationStartDate;
            campaign.RegistrationEndDate = dto.RegistrationEndDate;
            campaign.StartDate = dto.StartDate;
            campaign.EndDate = dto.EndDate;
            if (dto.IsActive != null) campaign.IsActive = dto.IsActive.Value;
            campaign.UpdatedAt = DateTime.UtcNow;

            var statusFieldsChanged = oldStartDate != campaign.StartDate
                                   || oldEndDate != campaign.EndDate
                                   || oldIsActive != campaign.IsActive;

            await _campaignRepo.UpdateAsync(campaign);

            if (statusFieldsChanged)
            {
                ScheduleCampaignStatusJobs(campaign.CampaignId, campaign.StartDate, campaign.EndDate);
            }

            // If EndDate changed on a system campaign, schedule a new expiration job.
            // The previously scheduled job will self-abort (EndDate > UtcNow guard).
            if (endDateChanged && campaign.CreatedByVendorId == null)
            {
                _backgroundJobClient.Schedule<IQuestExpirationJob>(
                    job => job.ExpireCampaignQuestsAsync(campaign.CampaignId),
                    ResolveHangfireRunAt(campaign.EndDate));
            }

            return await GetCampaignByIdAsync(campaign.CampaignId);
        }

        // --- Campaign Image Methods ---
    }
}