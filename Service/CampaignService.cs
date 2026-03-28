using BO.DTO.Campaigns;
using BO.Entities;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service
{
    public class CampaignService : ICampaignService
    {
        private readonly ICampaignRepository _campaignRepo;
        private readonly IBranchCampaignRepository _branchCampaignRepo;
        private readonly IBranchRepository _branchRepo;
        private readonly ITierRepository _tierRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IVendorRepository _vendorRepo;
        private readonly Service.PaymentsService.IPaymentService _paymentService;

        public CampaignService(
            ICampaignRepository campaignRepo,
            IBranchCampaignRepository branchCampaignRepo,
            IBranchRepository branchRepo,
            ITierRepository tierRepo,
            IPaymentRepository paymentRepo,
            IVendorRepository vendorRepo,
            Service.PaymentsService.IPaymentService paymentService)
        {
            _campaignRepo = campaignRepo;
            _branchCampaignRepo = branchCampaignRepo;
            _branchRepo = branchRepo;
            _tierRepo = tierRepo;
            _paymentRepo = paymentRepo;
            _vendorRepo = vendorRepo;
            _paymentService = paymentService;
        }

        public async Task<CampaignResponseDto> CreateVendorCampaignAsync(int userId, CreateVendorCampaignDto dto)
        {
            var vendor = await _vendorRepo.GetByUserIdAsync(userId);
            if (vendor == null)
                throw new DomainExceptions("Không tìm thấy Vendor của người dùng này.");

            var campaign = new Campaign
            {
                Name = dto.Name,
                Description = dto.Description,
                TargetSegment = dto.TargetSegment,
                RegistrationStartDate = null,
                RegistrationEndDate = null,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive,
                CreatedByVendorId = vendor.VendorId
            };
            await _campaignRepo.CreateAsync(campaign);

            // For vendor-created campaigns, create BranchCampaign rows so vouchers/orders
            // can validate join status using (branchId, campaignId) lookup.
            var branches = await _branchRepo.GetAllByVendorIdAsync(vendor.VendorId);
            foreach (var branch in branches)
            {
                // Eligibility matches the existing "system campaign join" rule.
                if (!(branch.Tier != null && branch.Tier.Weight >= 1 && branch.IsSubscribed))
                    continue;

                var existing = await _branchCampaignRepo.GetByBranchAndCampaignAsync(branch.BranchId, campaign.CampaignId);
                if (existing != null)
                    continue;

                await _branchCampaignRepo.CreateAsync(new BO.Entities.BranchCampaign
                {
                    BranchId = branch.BranchId,
                    CampaignId = campaign.CampaignId,
                    // If campaign is inactive, reflect that in join row as well.
                    IsActive = campaign.IsActive
                });
            }

            return await GetCampaignByIdAsync(campaign.CampaignId);
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

            return await GetCampaignByIdAsync(campaign.CampaignId);
        }

        public async Task<CampaignResponseDto> CreateRestaurantCampaignAsync(int userId, int branchId, CreateVendorCampaignDto dto)
        {
            var vendor = await _vendorRepo.GetByUserIdAsync(userId);
            if (vendor == null)
                throw new DomainExceptions("Không tìm thấy Vendor của người dùng này.");

            var branch = await _branchRepo.GetByIdAsync(branchId);
            if (branch == null || branch.VendorId != vendor.VendorId)
                throw new DomainExceptions("Không tìm thấy chi nhánh hoặc không có quyền truy cập.");

            var campaign = new Campaign
            {
                Name = dto.Name,
                Description = dto.Description,
                TargetSegment = dto.TargetSegment,
                RegistrationStartDate = null,
                RegistrationEndDate = null,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive,
                CreatedByBranchId = branchId
            };
            await _campaignRepo.CreateAsync(campaign);

            // For branch-created campaigns, create a BranchCampaign row for management UI
            // (even though vouchers/orders can also rely on CreatedByBranchId directly).
            var existing = await _branchCampaignRepo.GetByBranchAndCampaignAsync(branchId, campaign.CampaignId);
            if (existing == null)
            {
                await _branchCampaignRepo.CreateAsync(new BO.Entities.BranchCampaign
                {
                    BranchId = branchId,
                    CampaignId = campaign.CampaignId,
                    IsActive = campaign.IsActive
                });
            }

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

        public async Task<BO.Common.PaginatedResponse<CampaignResponseDto>> GetVendorCampaignsAsync(int userId, CampaignQueryDto query)
        {
            var vendor = await _vendorRepo.GetByUserIdAsync(userId);
            if (vendor == null) throw new DomainExceptions("Không tìm thấy Vendor của người dùng này.");

            var (items, totalCount) = await _campaignRepo.GetCampaignsAsync(false, vendor.VendorId, query.PageNumber, query.PageSize);
            
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
                    UpdatedAt = item.UpdatedAt
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
                    UpdatedAt = item.UpdatedAt
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
            var (items, totalCount) = await _campaignRepo.GetPublicCampaignsAsync(query.PageNumber, query.PageSize);
            
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
                    UpdatedAt = item.UpdatedAt
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

            campaign.Name = dto.Name;
            campaign.Description = dto.Description;
            campaign.TargetSegment = dto.TargetSegment;
            campaign.RegistrationStartDate = dto.RegistrationStartDate;
            campaign.RegistrationEndDate = dto.RegistrationEndDate;
            campaign.StartDate = dto.StartDate;
            campaign.EndDate = dto.EndDate;
            if (dto.IsActive != null) campaign.IsActive = dto.IsActive.Value;
            campaign.UpdatedAt = DateTime.UtcNow;

            await _campaignRepo.UpdateAsync(campaign);

            return await GetCampaignByIdAsync(campaign.CampaignId);
        }

        public async Task<BO.Common.PaginatedResponse<CampaignResponseDto>> GetCampaignsByBranchAsync(int userId, string role, int branchId, CampaignQueryDto query)
        {
            var branch = await _branchRepo.GetByIdAsync(branchId);
            if (branch == null) throw new DomainExceptions("Không tìm thấy chi nhánh.");

            if (role != "Admin")
            {
                if (role == "Vendor")
                {
                    var vendor = await _vendorRepo.GetByUserIdAsync(userId);
                    if (vendor == null || branch.VendorId != vendor.VendorId)
                        throw new DomainExceptions("Bạn không có quyền xem chiến dịch của chi nhánh này.");
                }
                else if (role == "Manager")
                {
                    if (branch.ManagerId != userId)
                        throw new DomainExceptions("Bạn không phải quản lý của chi nhánh này.");
                }
                else
                {
                    throw new DomainExceptions("Bạn không có quyền xem chiến dịch của chi nhánh này.");
                }
            }

            var (items, totalCount) = await _campaignRepo.GetCampaignsByBranchAsync(branchId, query.PageNumber, query.PageSize);

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
                    UpdatedAt = item.UpdatedAt
                });
            }
            
            return new BO.Common.PaginatedResponse<CampaignResponseDto>(
                mappedItems, 
                totalCount,
                query.PageNumber, 
                query.PageSize
            );
        }

        // --- Campaign Image Methods ---
    }
}