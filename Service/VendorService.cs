using BO.Common;
using BO.DTO.Dietary;
using BO.DTO.Vendor;
using BO.Entities;
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
                throw new Exception($"User with ID {userId} not found");
            }

            // Check if user already has a vendor
            var existingVendor = await _vendorRepository.GetByUserIdAsync(userId);
            if (existingVendor != null)
            {
                throw new Exception("User already has a vendor account");
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
                throw new Exception($"Vendor with ID {vendorId} not found");
            }

            return MapToResponseDto(vendor);
        }

        public async Task<VendorResponseDto> GetVendorByUserIdAsync(int userId)
        {
            var vendor = await _vendorRepository.GetByUserIdAsync(userId);
            if (vendor == null)
            {
                throw new Exception($"Vendor for user ID {userId} not found");
            }

            return MapToResponseDto(vendor);
        }

        public async Task<PaginatedResponse<VendorResponseDto>> GetAllVendorsAsync(int pageNumber, int pageSize)
        {
            var (vendors, totalCount) = await _vendorRepository.GetAllAsync(pageNumber, pageSize);
            var items = vendors.Select(MapToResponseDto).ToList();
            return new PaginatedResponse<VendorResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PaginatedResponse<VendorResponseDto>> GetActiveVendorsAsync(int pageNumber, int pageSize)
        {
            var (vendors, totalCount) = await _vendorRepository.GetActiveVendorsAsync(pageNumber, pageSize);
            var items = vendors.Select(MapToResponseDto).ToList();
            return new PaginatedResponse<VendorResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task DeleteVendorAsync(int vendorId)
        {
            var exists = await _vendorRepository.ExistsByIdAsync(vendorId);
            if (!exists)
            {
                throw new Exception($"Vendor with ID {vendorId} not found");
            }

            await _vendorRepository.DeleteAsync(vendorId);
        }

        public async Task<VendorResponseDto> UpdateVendorAsync(int userId, UpdateVendorDto updateVendorDto)
        {
            var vendor = await _vendorRepository.GetByUserIdAsync(userId);
            if (vendor == null)
            {
                throw new Exception($"Vendor for user ID {userId} not found");
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
                throw new Exception($"Vendor with ID {vendorId} not found");
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
                throw new Exception($"Vendor with ID {vendorId} not found");
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

        private VendorResponseDto MapToResponseDto(Vendor vendor)
        {
            var branches = _branchRepository.GetAllByVendorIdAsync(vendor.VendorId).Result;
            
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
                Branches = branches.Select(b =>
                {
                    var licenseRequest = _branchRepository.GetBranchRequestAsync(b.BranchId).Result;
                    var licenseUrls = new System.Collections.Generic.List<string>();
                    if (!string.IsNullOrEmpty(licenseRequest?.LicenseUrl))
                    {
                        if (licenseRequest.LicenseUrl.TrimStart().StartsWith("["))
                        {
                            try { licenseUrls = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(licenseRequest.LicenseUrl); }
                            catch { licenseUrls.Add(licenseRequest.LicenseUrl); }
                        }
                        else
                        {
                            licenseUrls.Add(licenseRequest.LicenseUrl);
                        }
                    }
                    return new BO.DTO.Branch.BranchResponseDto
                    {
                        BranchId = b.BranchId,
                        VendorId = b.VendorId ?? 0,
                        ManagerId = b.ManagerId,
                        Name = b.Name,
                        PhoneNumber = b.PhoneNumber,
                        Email = b.Email,
                        AddressDetail = b.AddressDetail,
                        Ward = b.Ward,
                        City = b.City,
                        Lat = b.Lat,
                        Long = b.Long,
                        CreatedAt = b.CreatedAt,
                        UpdatedAt = b.UpdatedAt,
                        IsVerified = b.IsVerified,
                        AvgRating = b.AvgRating,
                        TotalReviewCount = b.TotalReviewCount,
                        TotalRatingSum = b.TotalRatingSum,
                        BatchReviewCount = b.BatchReviewCount,
                        BatchRatingSum = b.BatchRatingSum,
                        IsActive = b.IsActive,
                        IsSubscribed = b.IsSubscribed,
                        SubscriptionExpiresAt = b.SubscriptionExpiresAt,
                        DaysRemaining = b.SubscriptionExpiresAt.HasValue
                            ? (int)Math.Ceiling((b.SubscriptionExpiresAt.Value - DateTime.UtcNow).TotalDays)
                            : null,
                        TierId = b.TierId,
                        TierName = b.Tier?.Name ?? "Silver",
                        LicenseUrls = licenseUrls,
                        LicenseStatus = licenseRequest?.Status.ToString(),
                        LicenseRejectReason = licenseRequest?.RejectReason
                    };
                }).ToList()
            };
        }
    }
}
