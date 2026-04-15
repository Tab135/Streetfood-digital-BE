using BO.Common;
using BO.DTO.Branch;
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
    public class BranchService : IBranchService
    {
        private readonly IBranchRepository _branchRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly IUserRepository _userRepository;
        private readonly IQuestProgressService _questProgressService;
        private readonly ISettingService _settingService;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;

        public BranchService(
            IBranchRepository branchRepository,
            IVendorRepository vendorRepository,
            IUserRepository userRepository,
            IQuestProgressService questProgressService,
            ISettingService settingService,
            IUserService userService,
            INotificationService notificationService)
        {
            _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
            _vendorRepository = vendorRepository ?? throw new ArgumentNullException(nameof(vendorRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _questProgressService = questProgressService ?? throw new ArgumentNullException(nameof(questProgressService));
            _settingService = settingService ?? throw new ArgumentNullException(nameof(settingService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public async Task<Branch> CreateBranchAsync(CreateBranchDto createBranchDto, int vendorId, int userId)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new DomainExceptions($"Không tìm thấy cửa hàng với ID {vendorId}");
            }

            if (vendor.UserId != userId)
            {
                throw new DomainExceptions("Người dùng không sở hữu cửa hàng này");
            }

            var branch = new Branch
            {
                VendorId = vendorId,
                ManagerId = vendor.UserId,
                Name = createBranchDto.Name,
                CreatedById = userId,
                PhoneNumber = createBranchDto.PhoneNumber,
                Email = createBranchDto.Email,
                AddressDetail = createBranchDto.AddressDetail,
                Ward = createBranchDto.Ward,
                City = createBranchDto.City,
                Lat = createBranchDto.Lat,
                Long = createBranchDto.Long,
                IsVerified = false,
                IsActive = false, // Not active until verified
                IsSubscribed = false,
                AvgRating = 0
            };

            var createdBranch = await _branchRepository.CreateAsync(branch);

            // Auto-create a pending register request for the new branch
            var branchRequest = new BranchRequest
            {
                BranchId = createdBranch.BranchId,
                Type = 1,
                LicenseUrl = null,
                Status = RegisterVendorStatusEnum.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _branchRepository.AddBranchRequestAsync(branchRequest);

            return createdBranch;
        }

        public async Task<BranchResponseDto> CreateUserBranchAsync(CreateUserBranchRequest request, int userId)
        {
            var ghostpinXP = _settingService.GetInt("ghostpinXP", 0);

            var branch = new Branch
            {
                VendorId = null,
                ManagerId = null,
                CreatedById = userId,
                Name = request.Name,
                AddressDetail = request.AddressDetail,
                Ward = request.Ward,
                City = request.City,
                Lat = request.Lat,
                Long = request.Long,
                IsVerified = false,
                IsActive = false,
                IsSubscribed = false,
                GhostpinXP = ghostpinXP > 0 ? ghostpinXP : null,
                AvgRating = 0
            };

            var createdBranch = await _branchRepository.CreateAsync(branch);

            // Auto-create a pending register request for the new branch (ghost pin)
            var branchRequest = new BranchRequest
            {
                BranchId = createdBranch.BranchId,
                Type = 0,
                LicenseUrl = null,
                Status = RegisterVendorStatusEnum.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            if (ghostpinXP > 0)
            {
                await _userService.AddXPAsync(userId, ghostpinXP);
            }

            var responseDto = await MapToResponseDtoAsync(createdBranch);

            // Set these fields to null to match the exact JSON output requirement
            responseDto.LicenseUrls = null;
            responseDto.LicenseStatus = null;
            responseDto.LicenseRejectReason = null;

            return responseDto;
        }

        
        
        // --- Replacing GhostPin logic natively with Branch ---
        public async Task<(string Message, int BranchId)> ClaimUserBranchAsync(int branchId, int userId, List<string> licenseUrls)
        {
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null) throw new DomainExceptions("Không tìm thấy chi nhánh");

            if (branch.VendorId != null)
                throw new DomainExceptions("Chi nhánh này đã được nhận hoặc thuộc sở hữu của một cửa hàng.");

            // Do NOT assign VendorId yet. Just set the ManagerId so we know WHO is claiming.
            // VendorId will be created/assigned upon Moderator approval in VerifyBranchAsync.
            branch.ManagerId = userId;
            branch.IsVerified = false; // Needs moderator approval
            branch.IsActive = false;
            branch.UpdatedAt = DateTime.UtcNow;

            await _branchRepository.UpdateAsync(branch);

            var licenseUrlJson = licenseUrls != null && licenseUrls.Count > 0 ? 
                                 System.Text.Json.JsonSerializer.Serialize(licenseUrls) : null;
                                 
            var registrationRequest = new BranchRequest
            {
                BranchId = branchId,
                Type = 2, // Claim branch
                LicenseUrl = licenseUrlJson,
                Status = RegisterVendorStatusEnum.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _branchRepository.AddBranchRequestAsync(registrationRequest);

            return ("Yêu cầu nhận chi nhánh đã được gửi. Đang chờ kiểm duyệt.", branch.BranchId);
        }
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371e3;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        public async Task<BranchResponseDto> GetBranchByIdAsync(int branchId)
        {
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
            {
                throw new DomainExceptions($"Không tìm thấy chi nhánh với ID {branchId}");
            }

            return await MapToResponseDtoAsync(branch);
        }

        public async Task<PaginatedResponse<BranchResponseDto>> GetMyGhostPinBranchesAsync(int userId, int pageNumber, int pageSize)
        {
            var (branches, totalCount) = await _branchRepository.GetByCreatedByIdAsync(userId, pageNumber, pageSize);
            var requests = await _branchRepository.GetRegisterRequestsByBranchIdsAsync(branches.Select(b => b.BranchId).ToList());
            var items = branches.Select(b => MapToResponseDto(b, requests.GetValueOrDefault(b.BranchId))).ToList();
            return new PaginatedResponse<BranchResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<BranchResponseDto> GetMyManagedBranchAsync(int managerUserId)
        {
            var branches = await _branchRepository.GetAllByManagerIdAsync(managerUserId);
            if (branches == null || branches.Count == 0)
            {
                throw new DomainExceptions("No branch assigned to this manager", "ERR_NOT_FOUND");
            }

            if (branches.Count > 1)
            {
                throw new DomainExceptions("Manager is assigned to more than one branch", "ERR_CONFLICT");
            }

            var branch = branches[0];
            var request = await _branchRepository.GetBranchRequestAsync(branch.BranchId);
            return MapToResponseDto(branch, request);
        }

        public async Task<PaginatedResponse<BranchResponseDto>> GetAllApprovedGhostPinsAsync(int pageNumber, int pageSize)
        {
            var (branches, totalCount) = await _branchRepository.GetAllApprovedGhostPinsAsync(pageNumber, pageSize);
            var requests = await _branchRepository.GetRegisterRequestsByBranchIdsAsync(branches.Select(b => b.BranchId).ToList());
            var items = branches.Select(b => MapToResponseDto(b, requests.GetValueOrDefault(b.BranchId))).ToList();
            return new PaginatedResponse<BranchResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PaginatedResponse<BranchResponseDto>> GetBranchesByVendorIdAsync(int vendorId, int pageNumber, int pageSize)
        {
            var (branches, totalCount) = await _branchRepository.GetByVendorIdAsync(vendorId, pageNumber, pageSize);
            var requests = await _branchRepository.GetRegisterRequestsByBranchIdsAsync(branches.Select(b => b.BranchId).ToList());
            var items = branches.Select(b => MapToResponseDto(b, requests.GetValueOrDefault(b.BranchId))).ToList();
            return new PaginatedResponse<BranchResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PaginatedResponse<BranchResponseDto>> GetAllBranchesAsync(int pageNumber, int pageSize)
        {
            var (branches, totalCount) = await _branchRepository.GetAllAsync(pageNumber, pageSize);
            var requests = await _branchRepository.GetRegisterRequestsByBranchIdsAsync(branches.Select(b => b.BranchId).ToList());
            var items = branches.Select(b => MapToResponseDto(b, requests.GetValueOrDefault(b.BranchId))).ToList();

            var verifierIds = items
                .Where(i => i.VerifiedBy.HasValue)
                .Select(i => i.VerifiedBy!.Value)
                .Distinct()
                .ToList();

            if (verifierIds.Count > 0)
            {
                var verifierUsernames = new Dictionary<int, string?>();
                foreach (var verifierId in verifierIds)
                {
                    var verifier = await _userRepository.GetUserById(verifierId);
                    verifierUsernames[verifierId] = verifier?.UserName;
                }

                foreach (var item in items)
                {
                    if (item.VerifiedBy.HasValue && verifierUsernames.TryGetValue(item.VerifiedBy.Value, out var username))
                    {
                        item.VerifiedByUserName = username;
                    }
                }
            }

            return new PaginatedResponse<BranchResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PaginatedResponse<BranchResponseDto>> GetActiveBranchesAsync(int pageNumber, int pageSize)
        {
            var (branches, totalCount) = await _branchRepository.GetActiveBranchesAsync(pageNumber, pageSize);
            // active-branches listing is public; license info isn't needed here
            var items = branches.Select(b => MapToResponseDto(b, null)).ToList();
            return new PaginatedResponse<BranchResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<BranchResponseDto> UpdateBranchAsync(int branchId, UpdateBranchDto updateBranchDto, int userId)
        {
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
            {
                throw new DomainExceptions($"Không tìm thấy chi nhánh với ID {branchId}");
            }

            // Allow vendor owner or assigned manager to update branch
            var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId ?? 0);
            var isVendorOwner = vendor != null && vendor.UserId == userId;
            var isBranchManager = branch.ManagerId.HasValue && branch.ManagerId.Value == userId;
            if (!isVendorOwner && !isBranchManager)
            {
                throw new DomainExceptions("Không có quyền: Bạn không quản lý chi nhánh này");
            }

            // Update only non-null fields
            if (!string.IsNullOrEmpty(updateBranchDto.Name))
                branch.Name = updateBranchDto.Name;

            if (!string.IsNullOrEmpty(updateBranchDto.PhoneNumber))
                branch.PhoneNumber = updateBranchDto.PhoneNumber;

            if (!string.IsNullOrEmpty(updateBranchDto.Email))
                branch.Email = updateBranchDto.Email;

            if (!string.IsNullOrEmpty(updateBranchDto.AddressDetail))
                branch.AddressDetail = updateBranchDto.AddressDetail;

            if (!string.IsNullOrEmpty(updateBranchDto.Ward))
                branch.Ward = updateBranchDto.Ward;

            if (!string.IsNullOrEmpty(updateBranchDto.City))
                branch.City = updateBranchDto.City;

            if (updateBranchDto.Lat.HasValue)
                branch.Lat = updateBranchDto.Lat.Value;

            if (updateBranchDto.Long.HasValue)
                branch.Long = updateBranchDto.Long.Value;

            await _branchRepository.UpdateAsync(branch);

            return await MapToResponseDtoAsync(branch);
        }

        public async Task DeleteBranchAsync(int branchId, int userId)
        {
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
            {
                throw new DomainExceptions($"Không tìm thấy chi nhánh với ID {branchId}");
            }

            var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId ?? 0);
            if (vendor == null || vendor.UserId != userId)
            {
                throw new DomainExceptions("Người dùng không sở hữu cửa hàng này");
            }

            // Check if this is the only branch
            var branches = await _branchRepository.GetAllByVendorIdAsync(branch.VendorId ?? 0);
            if (branches.Count <= 1)
            {
                throw new DomainExceptions("Không thể xóa chi nhánh cuối cùng. Một cửa hàng phải có ít nhất một chi nhánh.");
            }

            await _branchRepository.DeleteAsync(branchId);
        }

        public async Task<bool> UserOwnsBranchAsync(int branchId, int userId)
        {
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
            {
                return false;
            }

            var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId ?? 0);
            return vendor != null && vendor.UserId == userId;
        }

        public async Task<bool> AssignManagerAsync(int branchId, int managerId, int vendorUserId)
        {
            if (!await UserOwnsBranchAsync(branchId, vendorUserId))
            {
                throw new DomainExceptions("You are not authorized to assign a manager to this branch.", "ERR_UNAUTHORIZED");
            }

            var newManagerUser = await _userRepository.GetUserById(managerId);
            if (newManagerUser == null)
            {
                throw new DomainExceptions("The user to be assigned as manager does not exist.", "ERR_USER_NOT_FOUND");
            }
            else if (newManagerUser.Role == Role.Vendor || newManagerUser.Role == Role.Manager)
            {
                throw new DomainExceptions("The user to be assigned as manager must not be a vendor or a manager of another branch","ERR_UNAUTHORIZED");
            }

            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
            {
                throw new DomainExceptions("Branch not found.", "ERR_BRANCH_NOT_FOUND");
            }

            // If assigning another existing manager, demote the current branch manager first.
            if (newManagerUser.Role == Role.User && branch.ManagerId.HasValue && branch.ManagerId.Value != newManagerUser.Id)
            {
                var currentManagerUser = await _userRepository.GetUserById(branch.ManagerId.Value);
                if (currentManagerUser != null && currentManagerUser.Role == Role.Manager)
                {
                    currentManagerUser.Role = Role.User;
                    await _userRepository.UpdateAsync(currentManagerUser);
                }
            }

            branch.ManagerId = newManagerUser.Id;
            branch.UpdatedAt = DateTime.UtcNow;

            await _branchRepository.UpdateAsync(branch);

            // Promote to Manager if they are currently a regular User
            if (newManagerUser.Role == Role.User)
            {
                newManagerUser.Role = Role.Manager;
                await _userRepository.UpdateAsync(newManagerUser);
            }

            return true;
        }

        public async Task<PaginatedResponse<BranchResponseDto>> GetUnverifiedBranchesAsync(int pageNumber, int pageSize)
        {
            var (branches, totalCount) = await _branchRepository.GetUnverifiedBranchesAsync(pageNumber, pageSize);
            var requests = await _branchRepository.GetRegisterRequestsByBranchIdsAsync(branches.Select(b => b.BranchId).ToList());
            var items = branches.Select(b => MapToResponseDto(b, requests.GetValueOrDefault(b.BranchId))).ToList();
            return new PaginatedResponse<BranchResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<BranchRequest> SubmitBranchLicenseAsync(int branchId, List<string> licenseImagePaths, int userId)
        {
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
            {
                throw new DomainExceptions($"Không tìm thấy chi nhánh với ID {branchId}");
            }

            // Verify user can manage the branch (vendor owner or assigned manager)
            if (!await UserCanManageBranchAsync(branchId, userId))
            {
                throw new DomainExceptions("Không có quyền: Bạn không quản lý chi nhánh này");
            }

            // Serialize list of URLs to JSON
            var licenseUrlJson = System.Text.Json.JsonSerializer.Serialize(licenseImagePaths);

            // Fetch the latest request
            var existingRequest = await _branchRepository.GetBranchRequestAsync(branchId);

            // If there is an existing PENDING request (like the one created during /api/Vendor or /api/Branch/vendor), OVERWRITE its license URL
            if (existingRequest != null && existingRequest.Status == RegisterVendorStatusEnum.Pending)
            {
                existingRequest.LicenseUrl = licenseUrlJson;
                existingRequest.UpdatedAt = DateTime.UtcNow;

                await _branchRepository.UpdateBranchRequestAsync(existingRequest);
                return existingRequest;
            }
            else
            {
                // If it doesn't exist or is already Processed, Create a NEW request
                var registrationRequest = new BranchRequest
                {
                    BranchId = branchId,
                    Type = 1, // License verification
                    LicenseUrl = licenseUrlJson,
                    Status = RegisterVendorStatusEnum.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _branchRepository.AddBranchRequestAsync(registrationRequest);
                return registrationRequest;
            }
        }

        public async Task<BranchRequest> GetBranchLicenseStatusAsync(int branchId, int userId)
        {
            if (!await UserCanManageBranchAsync(branchId, userId))
            {
                throw new DomainExceptions("Không có quyền: Bạn không quản lý chi nhánh này");
            }

            var registrationRequest = await _branchRepository.GetBranchRequestAsync(branchId);
            if (registrationRequest == null)
            {
                throw new DomainExceptions($"Không tìm thấy yêu cầu đăng ký cho chi nhánh với ID {branchId}");
            }

            return registrationRequest;
        }

        public async Task<PaginatedResponse<PendingRegistrationDto>> GetPendingBranchRegistrationsAsync(int pageNumber, int pageSize, int? type = null)
        {
            var (pendingRequests, totalCount) = await _branchRepository.GetAllBranchRequestsAsync(pageNumber, pageSize, type);
            var items = pendingRequests
                .Select(r => new PendingRegistrationDto
                {
                    BranchRequestId = r.BranchRequestId,
                    BranchId = r.BranchId,
                    LicenseUrl = r.LicenseUrl,
                    Type = r.Type,
                    Status = r.Status,
                    VerifiedBy = r.VerifiedBy,
                    RejectReason = r.RejectReason,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    IsCreatedByOwner = r.Branch?.VendorId != null,
                    Branch = r.Branch == null ? null : new PendingRegistrationDto.PendingBranchInfo
                    {
                        BranchId = r.Branch.BranchId,
                        VendorId = r.Branch.VendorId ?? 0,
                        ManagerId = r.Branch.ManagerId,
                        CreatedById = r.Branch.CreatedById,
                        Name = r.Branch.Name,
                        PhoneNumber = r.Branch.PhoneNumber,
                        Email = r.Branch.Email,
                        AddressDetail = r.Branch.AddressDetail,
                        Ward = r.Branch.Ward,
                        City = r.Branch.City,
                        Lat = r.Branch.Lat,
                        Long = r.Branch.Long,
                        CreatedAt = r.Branch.CreatedAt,
                        UpdatedAt = r.Branch.UpdatedAt,
                        IsVerified = r.Branch.IsVerified,
                        AvgRating = r.Branch.AvgRating,
                        TotalReviewCount = r.Branch.TotalReviewCount,
                        TotalRatingSum = r.Branch.TotalRatingSum,
                        BatchReviewCount = r.Branch.BatchReviewCount,
                        BatchRatingSum = r.Branch.BatchRatingSum,
                        IsActive = r.Branch.IsActive,
                        IsSubscribed = r.Branch.IsSubscribed,
                        SubscriptionExpiresAt = r.Branch.SubscriptionExpiresAt,
                        TierId = r.Branch.TierId,
                        TierName = r.Branch.Tier?.Name ?? "Silver",
                        BranchImages = r.Branch.BranchImages?.Select(i => new BranchImageResponseDto
                        {
                            BranchImageId = i.BranchImageId,
                            ImageUrl = i.ImageUrl
                        }).ToList()
                    }
                }).ToList();
            return new PaginatedResponse<PendingRegistrationDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<bool> VerifyBranchAsync(int branchId, int verifierUserId)
        {
            await EnsureVerifierIsAdminOrModeratorAsync(verifierUserId);

            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
            {
                throw new DomainExceptions($"Không tìm thấy chi nhánh với ID {branchId}");
            }

            // Handle ghost pin / vendor creation claim mechanism upon verification
            if (branch.ManagerId.HasValue && branch.VendorId == null)
            {
                int userId = branch.ManagerId.Value;
                var claimingUser = await _userRepository.GetUserById(userId);
                
                var existingVendor = await _vendorRepository.GetByUserIdAsync(userId);
                if (existingVendor != null)
                {
                    // User is already a vendor. Assign branch to existing vendor profile.
                    branch.VendorId = existingVendor.VendorId;
                }
                else
                {
                    // User is not a vendor yet. Provide them with a new Vendor profile automatically.
                    var newVendor = new Vendor
                    {
                        UserId = userId,
                        Name = branch.Name,
                        IsActive = true
                    };
                    newVendor = await _vendorRepository.CreateAsync(newVendor);
                    branch.VendorId = newVendor.VendorId;

                    if (claimingUser != null && claimingUser.Role == Role.User)
                    {
                        claimingUser.Role = Role.Vendor;
                        await _userRepository.UpdateAsync(claimingUser);
                    }
                }
            }

            branch.IsVerified = true;
            branch.IsActive = true;
            branch.IsSubscribed = false; // "ko đóng tiền thì chỉ là branch bình thường chưa được isSubcribed"
            branch.TierId = 2; // Silver
            branch.BatchReviewCount = 0;
            branch.BatchRatingSum = 0;
            await _branchRepository.UpdateAsync(branch);

            // Update registration request status
            var registrationRequest = await _branchRepository.GetBranchRequestAsync(branchId);
            if (registrationRequest != null)
            {
                registrationRequest.Status = RegisterVendorStatusEnum.Accept;
                registrationRequest.VerifiedBy = verifierUserId;
                registrationRequest.UpdatedAt = DateTime.UtcNow;
                await _branchRepository.UpdateBranchRequestAsync(registrationRequest);
            }

            // Standard promote vendor owner behavior (legacy fallback)
            if (branch.VendorId.HasValue)
            {
                var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId.Value);
                if (vendor != null)
                {
                    var vendorOwner = await _userRepository.GetUserById(vendor.UserId);
                    if (vendorOwner != null && vendorOwner.Role == Role.User)
                    {
                        vendorOwner.Role = Role.Vendor;
                        await _userRepository.UpdateAsync(vendorOwner);
                    }
                }
            }

            var approvedRecipientId = await ResolveBranchNotificationRecipientUserIdAsync(branch);
            if (approvedRecipientId.HasValue)
            {
                await _notificationService.NotifyAsync(
                    approvedRecipientId.Value,
                    NotificationType.BranchVerificationStatus,
                    "Chi nhánh đã được duyệt",
                    $"Chi nhánh '{branch.Name}' của bạn đã được xác minh thành công.",
                    branch.BranchId,
                    new
                    {
                        type = "branch_verification_approved",
                        branchId = branch.BranchId,
                        branchName = branch.Name,
                        status = "ACCEPTED"
                    });
            }

            return true;
        }

        public async Task<bool> RejectBranchRegistrationAsync(int branchId, string rejectionReason, int verifierUserId)
        {
            await EnsureVerifierIsAdminOrModeratorAsync(verifierUserId);

            var registrationRequest = await _branchRepository.GetBranchRequestAsync(branchId);
            if (registrationRequest == null)
            {
                throw new DomainExceptions($"Không tìm thấy yêu cầu đăng ký cho chi nhánh với ID {branchId}");
            }

            registrationRequest.Status = RegisterVendorStatusEnum.Reject;
            registrationRequest.VerifiedBy = verifierUserId;
            registrationRequest.RejectReason = rejectionReason;
            registrationRequest.UpdatedAt = DateTime.UtcNow;
            await _branchRepository.UpdateBranchRequestAsync(registrationRequest);

            var branch = registrationRequest.Branch ?? await _branchRepository.GetByIdAsync(branchId);
            if (branch != null)
            {
                var rejectedRecipientId = await ResolveBranchNotificationRecipientUserIdAsync(branch);
                if (rejectedRecipientId.HasValue)
                {
                    var reason = string.IsNullOrWhiteSpace(rejectionReason)
                        ? "Không có lý do cụ thể"
                        : rejectionReason;

                    await _notificationService.NotifyAsync(
                        rejectedRecipientId.Value,
                        NotificationType.BranchVerificationStatus,
                        "Yêu cầu chi nhánh bị từ chối",
                        $"Yêu cầu xác minh chi nhánh '{branch.Name}' đã bị từ chối. Lý do: {reason}",
                        branch.BranchId,
                        new
                        {
                            type = "branch_verification_rejected",
                            branchId = branch.BranchId,
                            branchName = branch.Name,
                            status = "REJECTED",
                            reason
                        });
                }
            }

            return true;
        }

        public async Task<bool> IsVendorOwnedByUserAsync(int vendorId, int userId)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            return vendor != null && vendor.UserId == userId;
        }

        private BranchResponseDto MapToResponseDto(Branch branch, BranchRequest licenseRequest)
        {
            List<string> licenseUrls = null;
            if (licenseRequest != null)
            {
                if (!string.IsNullOrEmpty(licenseRequest.LicenseUrl))
                {
                    licenseUrls = new List<string>();
                    if (licenseRequest.LicenseUrl.TrimStart().StartsWith("["))
                    {
                        try { licenseUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(licenseRequest.LicenseUrl); }
                        catch { licenseUrls.Add(licenseRequest.LicenseUrl); }
                    }
                    else
                    {
                        licenseUrls.Add(licenseRequest.LicenseUrl);
                    }
                }
                else
                {
                    licenseUrls = new List<string>();
                }
            }
            return BuildResponseDto(branch, licenseRequest, licenseUrls);
        }

        private async Task<BranchResponseDto> MapToResponseDtoAsync(Branch branch)
        {
            var licenseRequest = await _branchRepository.GetBranchRequestAsync(branch.BranchId);
            return MapToResponseDto(branch, licenseRequest);
        }

        private BranchResponseDto BuildResponseDto(Branch branch, BranchRequest licenseRequest, List<string> licenseUrls)
        {
            return new BranchResponseDto
            {
                BranchId = branch.BranchId,
                VendorId = branch.VendorId ?? 0,
                VendorName = branch.Vendor?.Name ?? string.Empty,
                ManagerId = branch.ManagerId,
                Name = branch.Name,
                PhoneNumber = branch.PhoneNumber,
                Email = branch.Email,
                AddressDetail = branch.AddressDetail,
                Ward = branch.Ward,
                City = branch.City,
                Lat = branch.Lat,
                Long = branch.Long,
                CreatedAt = branch.CreatedAt,
                UpdatedAt = branch.UpdatedAt,
                IsVerified = branch.IsVerified,
                AvgRating = branch.AvgRating,
                TotalReviewCount = branch.TotalReviewCount,
                TotalRatingSum = branch.TotalRatingSum,
                BatchReviewCount = branch.BatchReviewCount,
                BatchRatingSum = branch.BatchRatingSum,
                GhostpinXP = branch.GhostpinXP,
                CreatedById = branch.CreatedById,
                LastTierResetAt = branch.LastTierResetAt,
                IsActive = branch.IsActive,
                IsSubscribed = branch.IsSubscribed,
                SubscriptionExpiresAt = branch.SubscriptionExpiresAt,
                DaysRemaining = branch.SubscriptionExpiresAt.HasValue
                    ? (int)Math.Ceiling((branch.SubscriptionExpiresAt.Value - DateTime.UtcNow).TotalDays)
                    : null,
                TierId = branch.TierId,
                TierName = branch.Tier?.Name ?? "Silver", // Default to Silver if null
                LicenseUrls = licenseUrls,
                LicenseStatus = licenseRequest?.Status.ToString(),
                VerifiedBy = licenseRequest?.VerifiedBy,
                LicenseRejectReason = licenseRequest?.RejectReason
            };
        }

        //private BO.DTO.Branch.BranchPublicDto MapToPublicDto(Branch branch)
        //{
        //    // Public version without vendor-specific fields
        //    return new BO.DTO.Branch.BranchPublicDto
        //    {
        //        BranchId = branch.BranchId,
        //        VendorId = branch.VendorId,
        //        ManagerId = branch.ManagerId,
        //        Name = branch.Name,
        //        PhoneNumber = branch.PhoneNumber,
        //        Email = branch.Email,
        //        AddressDetail = branch.AddressDetail,
        //        Ward = branch.Ward,
        //        City = branch.City,
        //        Lat = branch.Lat,
        //        Long = branch.Long,
        //        CreatedAt = branch.CreatedAt,
        //        UpdatedAt = branch.UpdatedAt,
        //        IsVerified = branch.IsVerified,
        //        AvgRating = branch.AvgRating,
        //        IsActive = branch.IsActive,
        //    };
        //}


        public async Task<List<WorkSchedule>> AddWorkScheduleAsync(int branchId, AddWorkScheduleDto dto, int userId)
        {
            // Verify user can manage the branch
            if (!await UserCanManageBranchAsync(branchId, userId))
            {
                throw new DomainExceptions("Không có quyền: Bạn không quản lý chi nhánh này");
            }

            var workSchedules = dto.Weekdays.Select(weekday => new WorkSchedule
            {
                BranchId = branchId,
                Weekday = weekday,
                OpenTime = dto.OpenTime,
                CloseTime = dto.CloseTime
            }).ToList();

            foreach (var workSchedule in workSchedules)
            {
                await _branchRepository.AddWorkScheduleAsync(workSchedule);
            }

            return workSchedules;
        }

        public async Task<List<WorkScheduleResponseDto>> GetBranchWorkSchedulesAsync(int branchId)
        {
            var schedules = await _branchRepository.GetWorkSchedulesAsync(branchId);
            return schedules.Select(s => new WorkScheduleResponseDto
            {
                WorkScheduleId = s.WorkScheduleId,
                BranchId = s.BranchId,
                Weekday = s.Weekday,
                WeekdayName = GetWeekdayName(s.Weekday),
                OpenTime = s.OpenTime,
                CloseTime = s.CloseTime
            }).ToList();
        }

        public async Task<WorkSchedule> UpdateWorkScheduleAsync(int scheduleId, UpdateWorkScheduleDto dto, int userId)
        {
            var schedule = await _branchRepository.GetWorkScheduleByIdAsync(scheduleId);
            if (schedule == null)
            {
                throw new DomainExceptions($"Không tìm thấy lịch làm việc với ID {scheduleId}");
            }

            // Verify user can manage the branch
            if (!await UserCanManageBranchAsync(schedule.BranchId, userId))
            {
                throw new DomainExceptions("Không có quyền: Bạn không quản lý chi nhánh này");
            }

            schedule.Weekday = dto.Weekday;
            schedule.OpenTime = dto.OpenTime;
            schedule.CloseTime = dto.CloseTime;

            await _branchRepository.UpdateWorkScheduleAsync(schedule);
            return schedule;
        }

        public async Task DeleteWorkScheduleAsync(int scheduleId, int userId)
        {
            var schedule = await _branchRepository.GetWorkScheduleByIdAsync(scheduleId);
            if (schedule == null)
            {
                throw new DomainExceptions($"Không tìm thấy lịch làm việc với ID {scheduleId}");
            }

            // Verify user can manage the branch
            if (!await UserCanManageBranchAsync(schedule.BranchId, userId))
            {
                throw new DomainExceptions("Không có quyền: Bạn không quản lý chi nhánh này");
            }

            await _branchRepository.DeleteWorkScheduleAsync(scheduleId);
        }


        public async Task<DayOff> AddDayOffAsync(int branchId, AddDayOffDto dto, int userId)
        {
            // Verify user can manage the branch
            if (!await UserCanManageBranchAsync(branchId, userId))
            {
                throw new DomainExceptions("Không có quyền: Bạn không quản lý chi nhánh này");
            }

            var dayOff = new DayOff
            {
                BranchId = branchId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime
            };

            await _branchRepository.AddDayOffAsync(dayOff);
            return dayOff;
        }

        public async Task<List<DayOffResponseDto>> GetBranchDayOffsAsync(int branchId)
        {
            var dayOffs = await _branchRepository.GetDayOffsAsync(branchId);
            return dayOffs.Select(d => new DayOffResponseDto
            {
                DayOffId = d.DayOffId,
                BranchId = d.BranchId,
                StartDate = d.StartDate,
                EndDate = d.EndDate,
                StartTime = d.StartTime,
                EndTime = d.EndTime
            }).ToList();
        }

        public async Task DeleteDayOffAsync(int dayOffId, int userId)
        {
            var dayOff = await _branchRepository.GetDayOffByIdAsync(dayOffId);
            if (dayOff == null)
            {
                throw new DomainExceptions($"Không tìm thấy ngày nghỉ với ID {dayOffId}");
            }

            // Verify user can manage the branch
            if (!await UserCanManageBranchAsync(dayOff.BranchId, userId))
            {
                throw new DomainExceptions("Không có quyền: Bạn không quản lý chi nhánh này");
            }

            await _branchRepository.DeleteDayOffAsync(dayOffId);
        }


        public async Task<BranchImage> AddBranchImageAsync(int branchId, string imageUrl, int userId)
        {
            // Verify user can manage the branch
            if (!await UserCanManageBranchAsync(branchId, userId))
            {
                throw new DomainExceptions("Không có quyền: Bạn không quản lý chi nhánh này");
            }

            var branchImage = new BranchImage
            {
                BranchId = branchId,
                ImageUrl = imageUrl
            };

            await _branchRepository.AddBranchImageAsync(branchImage);
            return branchImage;
        }

        public async Task<PaginatedResponse<BranchImageResponseDto>> GetBranchImagesAsync(int branchId, int pageNumber, int pageSize)
        {
            var (images, totalCount) = await _branchRepository.GetBranchImagesAsync(branchId, pageNumber, pageSize);
            var items = images.Select(i => new BranchImageResponseDto
            {
                BranchImageId = i.BranchImageId,
                ImageUrl = i.ImageUrl
            }).ToList();
            return new PaginatedResponse<BranchImageResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task DeleteBranchImageAsync(int imageId, int userId)
        {
            var image = await _branchRepository.GetBranchImageByIdAsync(imageId);
            if (image == null)
            {
                throw new DomainExceptions($"Không tìm thấy ảnh chi nhánh với ID {imageId}");
            }

            // Verify user can manage the branch
            if (!await UserCanManageBranchAsync(image.BranchId, userId))
            {
                throw new DomainExceptions("Không có quyền: Bạn không quản lý chi nhánh này");
            }

            await _branchRepository.DeleteBranchImageAsync(imageId);
        }

        public async Task<PaginatedResponse<SimilarBranchResponseDto>> GetSimilarBranchesByDishesAsync(int branchId, int pageNumber, int pageSize)
        {
            if (pageNumber <= 0)
            {
                throw new DomainExceptions("Page number must be greater than 0");
            }

            if (pageSize <= 0)
            {
                throw new DomainExceptions("Page size must be greater than 0");
            }

            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null || !branch.IsActive || !branch.IsVerified)
            {
                throw new DomainExceptions($"Không tìm thấy chi nhánh hoạt động với ID {branchId}");
            }

            var (items, totalCount) = await _branchRepository.GetSimilarBranchesByDishesAsync(branchId, pageNumber, pageSize);
            return new PaginatedResponse<SimilarBranchResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        private async Task EnsureVerifierIsAdminOrModeratorAsync(int verifierUserId)
        {
            var verifier = await _userRepository.GetUserById(verifierUserId);
            if (verifier == null)
            {
                throw new DomainExceptions("Không tìm thấy người xác minh");
            }

            if (verifier.Role != Role.Admin && verifier.Role != Role.Moderator)
            {
                throw new DomainExceptions("Chỉ Admin hoặc Moderator mới có quyền duyệt chi nhánh");
            }
        }

        private async Task<int?> ResolveBranchNotificationRecipientUserIdAsync(Branch branch)
        {
            if (branch.ManagerId.HasValue)
            {
                return branch.ManagerId.Value;
            }

            if (branch.VendorId.HasValue)
            {
                var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId.Value);
                if (vendor != null)
                {
                    return vendor.UserId;
                }
            }

            if (branch.CreatedById.HasValue)
            {
                return branch.CreatedById.Value;
            }

            return null;
        }

        // Helper method
        private async Task<bool> UserCanManageBranchAsync(int branchId, int userId)
        {
            if (await UserOwnsBranchAsync(branchId, userId))
            {
                return true;
            }

            var branch = await _branchRepository.GetByIdAsync(branchId);
            return branch != null && branch.ManagerId.HasValue && branch.ManagerId.Value == userId;
        }

        private string GetWeekdayName(int weekday)
        {
            return weekday switch
            {
                0 => "Sunday",
                1 => "Monday",
                2 => "Tuesday",
                3 => "Wednesday",
                4 => "Thursday",
                5 => "Friday",
                6 => "Saturday",
                _ => "Unknown"
            };
        }


        public async Task<ActiveBranchListResponseDto> GetActiveBranchesFilteredAsync(ActiveBranchFilterDto filter)
        {
            // Check if NO filters provided at all
            bool hasLatLong = filter.Lat.HasValue && filter.Long.HasValue;
            bool hasDistance = filter.Distance.HasValue;
            bool hasPrice = filter.MinPrice.HasValue || filter.MaxPrice.HasValue;
            bool hasTaste = filter.TasteIds != null && filter.TasteIds.Count > 0;
            bool hasDietary = filter.DietaryIds != null && filter.DietaryIds.Count > 0;
            bool hasCategory = filter.CategoryIds != null && filter.CategoryIds.Count > 0;
            bool hasAnyFilter = hasLatLong || hasDistance || hasPrice || hasTaste || hasDietary || hasCategory;

            // If NO filters provided, return all active branches without filtering
            if (!hasAnyFilter)
            {
                var allBranches = await _branchRepository.GetAllActiveBranchesWithoutFilterAsync();
                
                var allResponseDtos = allBranches.Select(branch =>
                {
                    var dishes = (branch.BranchDishes ?? new List<BranchDish>())
                        .Where(bd => bd.Dish != null && bd.Dish.IsActive)
                        .Select(bd => new { bd.Dish, bd.IsSoldOut });

                    double wDist = 0.6;
                    double wRate = 0.4;
                    double tierWeight = branch.Tier != null ? branch.Tier.Weight : 1.0;
                    double subMultiplier = branch.IsSubscribed ? 1.2 : 0.7;

                    double distanceScore = 0; // No location provided
                        
                    double ratingScore = (branch.AvgRating / 5) * wRate;

                    double finalScore = (distanceScore + ratingScore) * tierWeight * subMultiplier;

                    return new ActiveBranchResponseDto
                    {
                        BranchId      = branch.BranchId,
                        VendorId      = branch.VendorId ?? 0,
                        VendorName    = branch.Vendor?.Name ?? string.Empty,
                        Name          = branch.Name,
                        PhoneNumber   = branch.PhoneNumber,
                        Email         = branch.Email,
                        AddressDetail = branch.AddressDetail,
                        Ward          = branch.Ward,
                        City          = branch.City,
                        Lat           = branch.Lat,
                        Long          = branch.Long,
                        AvgRating = branch.AvgRating, TotalReviewCount = branch.TotalReviewCount, TotalRatingSum = branch.TotalRatingSum, IsVerified = branch.IsVerified, IsSubscribed = branch.IsSubscribed, TierId = branch.TierId, TierName = branch.Tier?.Name ?? "Silver", FinalScore = Math.Round(finalScore, 4), DistanceKm = null,
                        Dishes = dishes.Select(x => new ActiveDishResponseDto
                        {
                            DishId       = x.Dish.DishId,
                            Name         = x.Dish.Name,
                            Price        = x.Dish.Price,
                            Description  = x.Dish.Description,
                            ImageUrl     = x.Dish.ImageUrl,
                            IsSoldOut    = x.IsSoldOut,
                            CategoryName = x.Dish.Category?.Name ?? string.Empty,
                            TasteNames = x.Dish.DishTastes?
                                .Select(dt => dt.Taste?.Name ?? string.Empty)
                                .Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new(),
                        }).ToList(),
                        DietaryPreferenceNames = branch.Vendor?.VendorDietaryPreferences?
                            .Select(vdp => vdp.DietaryPreference?.Name ?? string.Empty)
                            .Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new()
                    };
                }).OrderByDescending(x => x.FinalScore).ToList();

                return new ActiveBranchListResponseDto
                {
                    Items      = allResponseDtos,
                    TotalCount = allResponseDtos.Count
                };
            }

            // If filters provided, use filtered logic
            if (filter.MinPrice.HasValue && filter.MaxPrice.HasValue && filter.MinPrice > filter.MaxPrice)
                throw new DomainExceptions("Giá tối thiểu không được lớn hơn giá tối đa");

            // User coordinates (nullable)
            double? userLat = filter.Lat;
            double? userLong = filter.Long;
            double? maxDistance = filter.Distance;

            // DAL handles ALL filtering logic (distance, price, taste, dietary, category)
            var items = await _branchRepository.GetActiveBranchesFilteredAsync(
                userLat, userLong, maxDistance,
                filter.DietaryIds, filter.TasteIds,
                filter.MinPrice, filter.MaxPrice,
                filter.CategoryIds);

            // Service layer only maps to DTOs - NO additional filtering
            var responseDtos = items.Select(item =>
            {
                var branch     = item.branch;
                var distanceKm = item.distanceKm;

                // Map all active dishes (already filtered by DAL)
                var dishes = (branch.BranchDishes ?? new List<BranchDish>())
                    .Where(bd => bd.Dish != null && bd.Dish.IsActive)
                    .Select(bd => new { bd.Dish, bd.IsSoldOut });

                double wDist = 0.6;
                double wRate = 0.4;
                double tierWeight = branch.Tier != null ? branch.Tier.Weight : 1.0;
                double subMultiplier = branch.IsSubscribed ? 1.2 : 0.7;

                double distanceScore = (distanceKm == 0 && (!userLat.HasValue || !userLong.HasValue)) 
                    ? 0 // If no user location, distance score is 0
                    : (1 / (distanceKm + 1)) * wDist;
                    
                double ratingScore = (branch.AvgRating / 5) * wRate;

                double finalScore = (distanceScore + ratingScore) * tierWeight * subMultiplier;

                return new ActiveBranchResponseDto
                {
                    BranchId      = branch.BranchId,
                    VendorId      = branch.VendorId ?? 0,
                    VendorName    = branch.Vendor?.Name ?? string.Empty,
                    Name          = branch.Name,
                    PhoneNumber   = branch.PhoneNumber,
                    Email         = branch.Email,
                    AddressDetail = branch.AddressDetail,
                    Ward          = branch.Ward,
                    City          = branch.City,
                    Lat           = branch.Lat,
                    Long          = branch.Long,
                    AvgRating = branch.AvgRating, TotalReviewCount = branch.TotalReviewCount, TotalRatingSum = branch.TotalRatingSum, IsVerified = branch.IsVerified, IsSubscribed = branch.IsSubscribed, TierId = branch.TierId, TierName = branch.Tier?.Name ?? "Silver", FinalScore = Math.Round(finalScore, 4), DistanceKm = Math.Round(distanceKm, 2),
                    Dishes = dishes.Select(x => new ActiveDishResponseDto
                    {
                        DishId       = x.Dish.DishId,
                        Name         = x.Dish.Name,
                        Price        = x.Dish.Price,
                        Description  = x.Dish.Description,
                        ImageUrl     = x.Dish.ImageUrl,
                        IsSoldOut    = x.IsSoldOut,
                        CategoryName = x.Dish.Category?.Name ?? string.Empty,
                        TasteNames = x.Dish.DishTastes?
                            .Select(dt => dt.Taste?.Name ?? string.Empty)
                            .Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new(),
                    }).ToList(),
                    DietaryPreferenceNames = branch.Vendor?.VendorDietaryPreferences?
                        .Select(vdp => vdp.DietaryPreference?.Name ?? string.Empty)
                        .Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new()
                };
            }).OrderByDescending(x => x.FinalScore).ToList();

            return new ActiveBranchListResponseDto
            {
                Items      = responseDtos,
                TotalCount = responseDtos.Count
            };
        }
    }
}






