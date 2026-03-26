using BO.DTO.Campaigns;
using BO.Entities;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace Service
{
    public class CampaignService : ICampaignService
    {
        private readonly ICampaignRepository _campaignRepo;
        private readonly IBranchCampaignRepository _branchCampaignRepo;
        private readonly IBranchRepository _branchRepo;

        public async Task UpdateCampaignImageUrlAsync(int campaignId, string? imageUrl, int userId, string role)
        {
            var campaign = await _campaignRepo.GetByIdAsync(campaignId);
            if (campaign == null) throw new DomainExceptions("Không tìm thấy chiến dịch.");

            // Quyền: Admin chỉ sửa campaign hệ thống, vendor chỉ sửa campaign của mình
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
        private readonly ITierRepository _tierRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IVendorRepository _vendorRepo;

        public CampaignService(
            ICampaignRepository campaignRepo,
            IBranchCampaignRepository branchCampaignRepo,
            IBranchRepository branchRepo,
            ITierRepository tierRepo,
            IPaymentRepository paymentRepo,
            IVendorRepository vendorRepo)
        {
            _campaignRepo = campaignRepo;
            _branchCampaignRepo = branchCampaignRepo;
            _branchRepo = branchRepo;
            _tierRepo = tierRepo;
            _paymentRepo = paymentRepo;
            _vendorRepo = vendorRepo;
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

            return await GetCampaignByIdAsync(campaign.CampaignId);
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
            if (campaign == null || campaign.CreatedByBranchId != null)
                throw new DomainExceptions("Chiến dịch hệ thống không tồn tại.");

            var now = DateTime.UtcNow;
            if (campaign.RegistrationStartDate.HasValue && campaign.RegistrationEndDate.HasValue)
            {
                if (now < campaign.RegistrationStartDate || now > campaign.RegistrationEndDate)
                    throw new DomainExceptions("Không nằm trong thời gian tham gia chiến dịch này.");
            }

            var existingJoin = await _branchCampaignRepo.GetByBranchAndCampaignAsync(branchId, campaignId);
            if (existingJoin != null)
            {
                if (existingJoin.IsActive)
                    throw new DomainExceptions("Chi nhánh đã tham gia và thanh toán chiến dịch này.");
                // Nếu chưa active, trả về ID để thanh toán lại
                return existingJoin.Id;
            }

            // Require minimum Tier for System Campaign (Weight >= 1)
            if (campaign.CreatedByBranchId == null && campaign.CreatedByVendorId == null)
            {
                if (branch.Tier == null || branch.Tier.Weight < 1)
                {
                    throw new DomainExceptions("Cấp bậc của chi nhánh không đủ điều kiện tham gia chiến dịch system (yêu cầu Tier mặc định trở lên).");
                }
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
            
            var mappedItems = new System.Collections.Generic.List<CampaignResponseDto>();
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
                query.PageNumber,
                query.PageSize,
                totalCount
            );
        }

        public async Task<BO.Common.PaginatedResponse<CampaignResponseDto>> GetVendorCampaignsAsync(int userId, CampaignQueryDto query)
        {
            var vendor = await _vendorRepo.GetByUserIdAsync(userId);
            if (vendor == null) throw new DomainExceptions("Không tìm thấy Vendor của người dùng này.");

            var (items, totalCount) = await _campaignRepo.GetCampaignsAsync(false, vendor.VendorId, query.PageNumber, query.PageSize);
            
            var mappedItems = new System.Collections.Generic.List<CampaignResponseDto>();
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
                    EndDate = item.EndDate, IsActive = item.IsActive,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt
                });
            }

            return new BO.Common.PaginatedResponse<CampaignResponseDto>(
                mappedItems,
                query.PageNumber,
                query.PageSize,
                totalCount
            );
        }

        public async Task<BO.Common.PaginatedResponse<CampaignResponseDto>> GetJoinableSystemCampaignsAsync(CampaignQueryDto query)
        {
            var (items, totalCount) = await _campaignRepo.GetJoinableSystemCampaignsAsync(query.PageNumber, query.PageSize);
            
            var mappedItems = new System.Collections.Generic.List<CampaignResponseDto>();
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
                    EndDate = item.EndDate, IsActive = item.IsActive,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt
                });
            }

            return new BO.Common.PaginatedResponse<CampaignResponseDto>(
                mappedItems,
                query.PageNumber,
                query.PageSize,
                totalCount
            );
        }

        public async Task<BO.Common.PaginatedResponse<CampaignResponseDto>> GetPublicCampaignsAsync(CampaignQueryDto query)
        {
            var (items, totalCount) = await _campaignRepo.GetPublicCampaignsAsync(query.PageNumber, query.PageSize);
            
            var mappedItems = new System.Collections.Generic.List<CampaignResponseDto>();
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
                    EndDate = item.EndDate, IsActive = item.IsActive,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt
                });
            }

            return new BO.Common.PaginatedResponse<CampaignResponseDto>(
                mappedItems,
                query.PageNumber,
                query.PageSize,
                totalCount
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

                // Allow update if the vendor created it OR if a branch owned by this vendor created it
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

            var mappedItems = new System.Collections.Generic.List<CampaignResponseDto>();
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
                    EndDate = item.EndDate, IsActive = item.IsActive,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt
                });
            }
            return new BO.Common.PaginatedResponse<CampaignResponseDto>(mappedItems, totalCount, query.PageNumber, query.PageSize);
        }

        // --- Campaign Image Methods ---
    }
}
