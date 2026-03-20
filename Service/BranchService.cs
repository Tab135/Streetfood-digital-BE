using BO.Common;
using BO.DTO.Branch;
using BO.Entities;
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

        public BranchService(
            IBranchRepository branchRepository,
            IVendorRepository vendorRepository,
            IUserRepository userRepository)
        {
            _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
            _vendorRepository = vendorRepository ?? throw new ArgumentNullException(nameof(vendorRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<Branch> CreateBranchAsync(CreateBranchDto createBranchDto, int vendorId, int userId)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new Exception($"Vendor with ID {vendorId} not found");
            }

            if (vendor.UserId != userId)
            {
                throw new Exception("User does not own this vendor");
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
            var branchRegisterRequest = new BranchRegisterRequest
            {
                BranchId = createdBranch.BranchId,
                LicenseUrl = null,
                Status = RegisterVendorStatusEnum.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _branchRepository.AddBranchRegisterRequestAsync(branchRegisterRequest);

            return createdBranch;
        }

        public async Task<BranchResponseDto> CreateUserBranchAsync(CreateUserBranchRequest request, int userId)
        {
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
                AvgRating = 0
            };

            var createdBranch = await _branchRepository.CreateAsync(branch);

            // Auto-create a pending register request for the new branch (ghost pin)
            var branchRegisterRequest = new BranchRegisterRequest
            {
                BranchId = createdBranch.BranchId,
                LicenseUrl = null,
                Status = RegisterVendorStatusEnum.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _branchRepository.AddBranchRegisterRequestAsync(branchRegisterRequest);

            var responseDto = await MapToResponseDtoAsync(createdBranch);
            
            // Set these fields to null to match the exact JSON output requirement
            responseDto.LicenseUrls = null;
            responseDto.LicenseStatus = null;
            responseDto.LicenseRejectReason = null;

            return responseDto;
        }

        
        
        // --- Replacing GhostPin logic natively with Branch ---
        public async Task<object> ClaimUserBranchAsync(int branchId, int vendorId, int userId, ClaimUserBranchRequest request)
        {
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null) throw new Exception("Branch not found");
            if (!branch.IsVerified || branch.VendorId != null)
                throw new Exception("Only verified and unowned branches can be claimed.");

            if (request.ExistingBranchId.HasValue)
            {
                var targetBranch = await _branchRepository.GetByIdAsync(request.ExistingBranchId.Value);
                if (targetBranch == null || targetBranch.VendorId != vendorId)
                    throw new Exception("Invalid or unauthorized existing branch.");

                // Merge data
                targetBranch.Lat = branch.Lat;
                targetBranch.Long = branch.Long;
                targetBranch.AddressDetail = branch.AddressDetail;
                targetBranch.Ward = branch.Ward;
                targetBranch.City = branch.City;
                targetBranch.UpdatedAt = DateTime.UtcNow;

                await _branchRepository.UpdateAsync(targetBranch);

                await _branchRepository.DeleteAsync(branch.BranchId);

                return new { Message = "Merged with existing branch. Generating payment link...", BranchId = targetBranch.BranchId };
            }
            else
            {
                branch.VendorId = vendorId;
                branch.ManagerId = userId;
                branch.UpdatedAt = DateTime.UtcNow;
                await _branchRepository.UpdateAsync(branch);

                return new { Message = "Claiming new branch. Generating payment link...", BranchId = branch.BranchId };
            }
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
                throw new Exception($"Branch with ID {branchId} not found");
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
                throw new Exception($"Branch with ID {branchId} not found");
            }

            // Verify vendor exists and user owns it
            var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId ?? 0);
            if (vendor == null || vendor.UserId != userId)
            {
                throw new Exception("User does not own this vendor");
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
                throw new Exception($"Branch with ID {branchId} not found");
            }

            var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId ?? 0);
            if (vendor == null || vendor.UserId != userId)
            {
                throw new Exception("User does not own this vendor");
            }

            // Check if this is the only branch
            var branches = await _branchRepository.GetAllByVendorIdAsync(branch.VendorId ?? 0);
            if (branches.Count <= 1)
            {
                throw new Exception("Cannot delete the last branch. A vendor must have at least one branch.");
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

        public async Task<PaginatedResponse<BranchResponseDto>> GetUnverifiedBranchesAsync(int pageNumber, int pageSize)
        {
            var (branches, totalCount) = await _branchRepository.GetUnverifiedBranchesAsync(pageNumber, pageSize);
            var requests = await _branchRepository.GetRegisterRequestsByBranchIdsAsync(branches.Select(b => b.BranchId).ToList());
            var items = branches.Select(b => MapToResponseDto(b, requests.GetValueOrDefault(b.BranchId))).ToList();
            return new PaginatedResponse<BranchResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<BranchRegisterRequest> SubmitBranchLicenseAsync(int branchId, List<string> licenseImagePaths, int userId)
        {
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
            {
                throw new Exception($"Branch with ID {branchId} not found");
            }

            // Verify user owns the branch
            if (!await UserOwnsBranchAsync(branchId, userId))
            {
                throw new Exception("User does not own this branch");
            }

            // Check if registration request already exists
            var existingRequest = await _branchRepository.GetBranchRegisterRequestAsync(branchId);
            
            // Serialize list of URLs to JSON
            var licenseUrlJson = System.Text.Json.JsonSerializer.Serialize(licenseImagePaths);

            var registrationRequest = new BranchRegisterRequest
            {
                BranchId = branchId,
                LicenseUrl = licenseUrlJson,
                Status = RegisterVendorStatusEnum.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (existingRequest != null)
            {
                registrationRequest.BranchRegisterRequestId = existingRequest.BranchRegisterRequestId;
                existingRequest.LicenseUrl = licenseUrlJson;
                existingRequest.Status = RegisterVendorStatusEnum.Pending;
                existingRequest.UpdatedAt = DateTime.UtcNow;
                
                await _branchRepository.UpdateBranchRegisterRequestAsync(existingRequest);
                return existingRequest; // Return the updated entity
            }
            else
            {
                await _branchRepository.AddBranchRegisterRequestAsync(registrationRequest);
                return registrationRequest;
            }
        }

        public async Task<BranchRegisterRequest> GetBranchLicenseStatusAsync(int branchId, int userId)
        {
            if (!await UserOwnsBranchAsync(branchId, userId))
            {
                throw new Exception("User does not own this branch");
            }

            var registrationRequest = await _branchRepository.GetBranchRegisterRequestAsync(branchId);
            if (registrationRequest == null)
            {
                throw new Exception($"No registration request found for branch ID {branchId}");
            }

            return registrationRequest;
        }

        public async Task<PaginatedResponse<PendingRegistrationDto>> GetPendingBranchRegistrationsAsync(int pageNumber, int pageSize)
        {
            var (pendingRequests, totalCount) = await _branchRepository.GetAllBranchRegisterRequestsAsync(pageNumber, pageSize);
            var items = pendingRequests
                .Select(r => new PendingRegistrationDto
                {
                    BranchRegisterRequestId = r.BranchRegisterRequestId,
                    BranchId = r.BranchId,
                    LicenseUrl = r.LicenseUrl,
                    Status = r.Status,
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

        public async Task<bool> VerifyBranchAsync(int branchId)
        {
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
            {
                throw new Exception($"Branch with ID {branchId} not found");
            }

            branch.IsVerified = true;
            branch.IsActive = true;
            branch.TierId = 2; // Silver
            branch.BatchReviewCount = 0;
            branch.BatchRatingSum = 0;
            await _branchRepository.UpdateAsync(branch);

            // Update registration request status
            var registrationRequest = await _branchRepository.GetBranchRegisterRequestAsync(branchId);
            if (registrationRequest != null)
            {
                registrationRequest.Status = RegisterVendorStatusEnum.Accept;
                registrationRequest.UpdatedAt = DateTime.UtcNow;
                await _branchRepository.UpdateBranchRegisterRequestAsync(registrationRequest);
            }

            // Promote vendor owner to Vendor role if not already
            var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId ?? 0);
            if (vendor != null)
            {
                var vendorOwner = await _userRepository.GetUserById(vendor.UserId);
                if (vendorOwner != null && vendorOwner.Role == Role.User)
                {
                    vendorOwner.Role = Role.Vendor;
                    await _userRepository.UpdateAsync(vendorOwner);
                }
            }

            return true;
        }

        public async Task<bool> RejectBranchRegistrationAsync(int branchId, string rejectionReason)
        {
            var registrationRequest = await _branchRepository.GetBranchRegisterRequestAsync(branchId);
            if (registrationRequest == null)
            {
                throw new Exception($"No registration request found for branch ID {branchId}");
            }

            registrationRequest.Status = RegisterVendorStatusEnum.Reject;
            registrationRequest.RejectReason = rejectionReason;
            registrationRequest.UpdatedAt = DateTime.UtcNow;
            await _branchRepository.UpdateBranchRegisterRequestAsync(registrationRequest);

            return true;
        }

        public async Task<bool> IsVendorOwnedByUserAsync(int vendorId, int userId)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            return vendor != null && vendor.UserId == userId;
        }

        private BranchResponseDto MapToResponseDto(Branch branch, BranchRegisterRequest licenseRequest)
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
            var licenseRequest = await _branchRepository.GetBranchRegisterRequestAsync(branch.BranchId);
            return MapToResponseDto(branch, licenseRequest);
        }

        private BranchResponseDto BuildResponseDto(Branch branch, BranchRegisterRequest licenseRequest, List<string> licenseUrls)
        {
            return new BranchResponseDto
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
                TierName = branch.Tier?.Name ?? "Silver", // Default to Silver if null
                LicenseUrls = licenseUrls,
                LicenseStatus = licenseRequest?.Status.ToString(),
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
            // Verify branch exists and user owns it
            if (!await UserOwnsBranchAsync(branchId, userId))
            {
                throw new Exception("Unauthorized: You do not own this branch");
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
                throw new Exception($"Work schedule with ID {scheduleId} not found");
            }

            // Verify user owns the branch
            if (!await UserOwnsBranchAsync(schedule.BranchId, userId))
            {
                throw new Exception("Unauthorized: You do not own this branch");
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
                throw new Exception($"Work schedule with ID {scheduleId} not found");
            }

            // Verify user owns the branch
            if (!await UserOwnsBranchAsync(schedule.BranchId, userId))
            {
                throw new Exception("Unauthorized: You do not own this branch");
            }

            await _branchRepository.DeleteWorkScheduleAsync(scheduleId);
        }


        public async Task<DayOff> AddDayOffAsync(int branchId, AddDayOffDto dto, int userId)
        {
            // Verify branch exists and user owns it
            if (!await UserOwnsBranchAsync(branchId, userId))
            {
                throw new Exception("Unauthorized: You do not own this branch");
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
                throw new Exception($"Day off with ID {dayOffId} not found");
            }

            // Verify user owns the branch
            if (!await UserOwnsBranchAsync(dayOff.BranchId, userId))
            {
                throw new Exception("Unauthorized: You do not own this branch");
            }

            await _branchRepository.DeleteDayOffAsync(dayOffId);
        }


        public async Task<BranchImage> AddBranchImageAsync(int branchId, string imageUrl, int userId)
        {
            // Verify branch exists and user owns it
            if (!await UserOwnsBranchAsync(branchId, userId))
            {
                throw new Exception("Unauthorized: You do not own this branch");
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
                throw new Exception($"Branch image with ID {imageId} not found");
            }

            // Verify user owns the branch
            if (!await UserOwnsBranchAsync(image.BranchId, userId))
            {
                throw new Exception("Unauthorized: You do not own this branch");
            }

            await _branchRepository.DeleteBranchImageAsync(imageId);
        }

        // Helper method
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
                    var dishes = branch.BranchDishes
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
                        AvgRating = branch.AvgRating, TotalReviewCount = branch.TotalReviewCount, TotalRatingSum = branch.TotalRatingSum, IsVerified = branch.IsVerified, TierId = branch.TierId, TierName = branch.Tier?.Name ?? "Silver", FinalScore = Math.Round(finalScore, 4), DistanceKm = null,
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
                throw new Exception("MinPrice cannot be greater than MaxPrice");

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
                var dishes = branch.BranchDishes
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
                    AvgRating = branch.AvgRating, TotalReviewCount = branch.TotalReviewCount, TotalRatingSum = branch.TotalRatingSum, IsVerified = branch.IsVerified, TierId = branch.TierId, TierName = branch.Tier?.Name ?? "Silver", FinalScore = Math.Round(finalScore, 4), DistanceKm = Math.Round(distanceKm, 2),
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




