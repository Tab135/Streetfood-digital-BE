using BO.Common;
using BO.DTO.Dietary;
using BO.DTO.Vendor;
using BO.Entities;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service
{
    public class VendorService : IVendorService
    {
        private readonly IVendorRepository _vendorRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBranchRepository _branchRepository;

        public VendorService(
            IVendorRepository vendorRepository,
            IUserRepository userRepository,
            IBranchRepository branchRepository)
        {
            _vendorRepository = vendorRepository ?? throw new ArgumentNullException(nameof(vendorRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
        }

        public async Task<Vendor> CreateVendorAsync(CreateVendorDto createVendorDto, int userId)
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
            {
                throw new DomainExceptions($"Không tìm thấy người dùng với ID {userId}");
            }

            // Check if user already has a vendor
            var existingVendor = await _vendorRepository.GetByUserIdAsync(userId);
            if (existingVendor != null)
            {
                throw new DomainExceptions("Đã có tài khoản cửa hàng cho người dùng này");
            }

            var vendor = new Vendor
            {
                UserId = userId,
                Name = createVendorDto.Name,
                IsActive = true
            };

            var createdVendor = await _vendorRepository.CreateAsync(vendor);

            // Create default branch with vendor name
            var defaultBranch = new Branch
            {
                VendorId = createdVendor.VendorId,
                ManagerId = userId,
                Name = !string.IsNullOrWhiteSpace(createVendorDto.BranchName) ? createVendorDto.BranchName : "1",
                PhoneNumber = createVendorDto.PhoneNumber,
                Email = createVendorDto.Email,
                AddressDetail = createVendorDto.AddressDetail,
                Ward = createVendorDto.Ward,
                City = createVendorDto.City,
                Lat = createVendorDto.Lat,
                Long = createVendorDto.Long,
                IsVerified = false,
                IsActive = false, // Not active until verified
                IsSubscribed = false,
                AvgRating = 0,
                CreatedById = userId
            };

            var createdBranch = await _branchRepository.CreateAsync(defaultBranch);

            //TODO: We fucking create this bitch ass branch request before those fucker upload license 
            var defaultRegisterRequest = new BranchRequest
            {
                BranchId = createdBranch.BranchId,
                RequestedByUserId = userId,
                Type = 1,
                LicenseUrl = null,
                Status = RegisterVendorStatusEnum.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _branchRepository.AddBranchRequestAsync(defaultRegisterRequest);

            return createdVendor;
        }

        public async Task<VendorResponseDto> GetVendorByIdAsync(int vendorId)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new DomainExceptions($"Không tìm thấy cửa hàng với ID {vendorId}");
            }

            return await MapToResponseDtoAsync(vendor);
        }

        public async Task<VendorResponseDto> GetVendorByUserIdAsync(int userId)
        {
            var vendor = await _vendorRepository.GetByUserIdAsync(userId);
            if (vendor == null)
            {
                throw new DomainExceptions($"Không tìm thấy cửa hàng của người dùng với ID {userId}");
            }

            return await MapToResponseDtoAsync(vendor);
        }

        public async Task<PaginatedResponse<VendorResponseDto>> GetAllVendorsAsync(int pageNumber, int pageSize)
        {
            var (vendors, totalCount) = await _vendorRepository.GetAllAsync(pageNumber, pageSize);
            var items = new List<VendorResponseDto>();

            foreach (var vendor in vendors)
            {
                items.Add(await MapToResponseDtoAsync(vendor));
            }

            return new PaginatedResponse<VendorResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PaginatedResponse<VendorResponseDto>> GetActiveVendorsAsync(int pageNumber, int pageSize)
        {
            var (vendors, totalCount) = await _vendorRepository.GetActiveVendorsAsync(pageNumber, pageSize);
            var items = new List<VendorResponseDto>();

            foreach (var vendor in vendors)
            {
                items.Add(await MapToResponseDtoAsync(vendor));
            }

            return new PaginatedResponse<VendorResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task DeleteVendorAsync(int vendorId)
        {
            var exists = await _vendorRepository.ExistsByIdAsync(vendorId);
            if (!exists)
            {
                throw new DomainExceptions($"Không tìm thấy cửa hàng với ID {vendorId}");
            }

            await _vendorRepository.DeleteAsync(vendorId);
        }

        public async Task<VendorResponseDto> UpdateVendorAsync(int userId, UpdateVendorDto updateVendorDto)
        {
            var vendor = await _vendorRepository.GetByUserIdAsync(userId);
            if (vendor == null)
            {
                throw new DomainExceptions($"Không tìm thấy cửa hàng của người dùng với ID {userId}");
            }

            vendor.Name = updateVendorDto.Name;
            vendor.UpdatedAt = DateTime.UtcNow;
            await _vendorRepository.UpdateAsync(vendor);

            return await GetVendorByIdAsync(vendor.VendorId);
        }

        public async Task<bool> SuspendVendorAsync(int vendorId)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new DomainExceptions($"Không tìm thấy cửa hàng với ID {vendorId}");
            }

            vendor.IsActive = false;
            vendor.UpdatedAt = DateTime.UtcNow;
            await _vendorRepository.UpdateAsync(vendor);

            // Deactivate all branches belonging to this vendor
            var branches = await _branchRepository.GetAllByVendorIdAsync(vendorId);
            foreach (var branch in branches)
            {
                if (branch.IsActive)
                {
                    branch.IsActive = false;
                    branch.UpdatedAt = DateTime.UtcNow;
                    await _branchRepository.UpdateAsync(branch);
                }
            }

            return true;
        }

        public async Task<bool> ReactivateVendorAsync(int vendorId)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new DomainExceptions($"Không tìm thấy cửa hàng với ID {vendorId}");
            }

            vendor.IsActive = true;
            vendor.UpdatedAt = DateTime.UtcNow;
            await _vendorRepository.UpdateAsync(vendor);

            // Reactivate only branches that have an active subscription
            var branches = await _branchRepository.GetAllByVendorIdAsync(vendorId);
            foreach (var branch in branches)
            {
                if (!branch.IsActive && branch.IsSubscribed)
                {
                    branch.IsActive = true;
                    branch.UpdatedAt = DateTime.UtcNow;
                    await _branchRepository.UpdateAsync(branch);
                }
            }

            return true;
        }

        private async Task<VendorResponseDto> MapToResponseDtoAsync(Vendor vendor)
        {
            var branches = await _branchRepository.GetAllByVendorIdAsync(vendor.VendorId);
            var branchRequests = branches.Count > 0
                ? await _branchRepository.GetRegisterRequestsByBranchIdsAsync(branches.Select(b => b.BranchId).ToList())
                : new Dictionary<int, BranchRequest>();

            var branchResponses = branches
                .Select(b => MapBranchToResponseDto(b, branchRequests.GetValueOrDefault(b.BranchId)))
                .ToList();

            var verifierIds = branchResponses
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

                foreach (var branchResponse in branchResponses)
                {
                    if (branchResponse.VerifiedBy.HasValue && verifierUsernames.TryGetValue(branchResponse.VerifiedBy.Value, out var username))
                    {
                        branchResponse.VerifiedByUserName = username;
                    }
                }
            }
            
            return new VendorResponseDto
            {
                VendorId = vendor.VendorId,
                UserId = vendor.UserId,
                Name = vendor.Name,
                CreatedAt = vendor.CreatedAt,
                UpdatedAt = vendor.UpdatedAt,
                IsActive = vendor.IsActive,
                VendorOwnerName = vendor.VendorOwner != null ? $"{vendor.VendorOwner.FirstName} {vendor.VendorOwner.LastName}".Trim() : "",
                DietaryPreferences = vendor.VendorDietaryPreferences
                    .Select(vdp => new DietaryPreferenceDto
                    {
                        DietaryPreferenceId = vdp.DietaryPreference.DietaryPreferenceId,
                        Name = vdp.DietaryPreference.Name,
                        Description = vdp.DietaryPreference.Description
                    }).ToList(),
                Branches = branchResponses
            };
        }

        private static BO.DTO.Branch.BranchResponseDto MapBranchToResponseDto(Branch branch, BranchRequest? licenseRequest)
        {
            var licenseUrls = new List<string>();
            if (!string.IsNullOrEmpty(licenseRequest?.LicenseUrl))
            {
                if (licenseRequest.LicenseUrl.TrimStart().StartsWith("["))
                {
                    try
                    {
                        licenseUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(licenseRequest.LicenseUrl) ?? new List<string>();
                    }
                    catch
                    {
                        licenseUrls.Add(licenseRequest.LicenseUrl);
                    }
                }
                else
                {
                    licenseUrls.Add(licenseRequest.LicenseUrl);
                }
            }

            return new BO.DTO.Branch.BranchResponseDto
            {
                BranchId = branch.BranchId,
                VendorId = branch.VendorId ?? 0,
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
                IsActive = branch.IsActive,
                IsSubscribed = branch.IsSubscribed,
                SubscriptionExpiresAt = branch.SubscriptionExpiresAt,
                DaysRemaining = branch.SubscriptionExpiresAt.HasValue
                    ? (int)Math.Ceiling((branch.SubscriptionExpiresAt.Value - DateTime.UtcNow).TotalDays)
                    : null,
                TierId = branch.TierId,
                TierName = branch.Tier?.Name ?? "Silver",
                LicenseUrls = licenseUrls,
                LicenseStatus = licenseRequest?.Status.ToString(),
                VerifiedBy = licenseRequest?.VerifiedBy,
                VerifiedByUserName = null,
                LicenseRejectReason = licenseRequest?.RejectReason
            };
        }
    }
}
