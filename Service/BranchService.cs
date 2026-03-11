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

            return await _branchRepository.CreateAsync(branch);
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

        public async Task<PaginatedResponse<BranchResponseDto>> GetBranchesByVendorIdAsync(int vendorId, int pageNumber, int pageSize)
        {
            var (branches, totalCount) = await _branchRepository.GetByVendorIdAsync(vendorId, pageNumber, pageSize);
            var items = new List<BranchResponseDto>();
            foreach (var branch in branches)
                items.Add(await MapToResponseDtoAsync(branch));
            return new PaginatedResponse<BranchResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PaginatedResponse<BranchResponseDto>> GetAllBranchesAsync(int pageNumber, int pageSize)
        {
            var (branches, totalCount) = await _branchRepository.GetAllAsync(pageNumber, pageSize);
            var items = new List<BranchResponseDto>();
            foreach (var branch in branches)
                items.Add(await MapToResponseDtoAsync(branch));
            return new PaginatedResponse<BranchResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PaginatedResponse<BranchResponseDto>> GetActiveBranchesAsync(int pageNumber, int pageSize)
        {
            var (branches, totalCount) = await _branchRepository.GetActiveBranchesAsync(pageNumber, pageSize);
            var items = new List<BranchResponseDto>();
            foreach (var branch in branches)
                items.Add(await MapToResponseDtoAsync(branch));
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
            var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId);
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

            var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId);
            if (vendor == null || vendor.UserId != userId)
            {
                throw new Exception("User does not own this vendor");
            }

            // Check if this is the only branch
            var branches = await _branchRepository.GetAllByVendorIdAsync(branch.VendorId);
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

            var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId);
            return vendor != null && vendor.UserId == userId;
        }

        public async Task<List<BranchResponseDto>> GetVerifiedBranchesAsync()
        {
            var branches = await _branchRepository.GetByVerificationStatusAsync(true);
            var items = new List<BranchResponseDto>();
            foreach (var branch in branches)
                items.Add(await MapToResponseDtoAsync(branch));
            return items;
        }

        public async Task<PaginatedResponse<BranchResponseDto>> GetUnverifiedBranchesAsync(int pageNumber, int pageSize)
        {
            var (branches, totalCount) = await _branchRepository.GetUnverifiedBranchesAsync(pageNumber, pageSize);
            var items = new List<BranchResponseDto>();
            foreach (var branch in branches)
                items.Add(await MapToResponseDtoAsync(branch));
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

        public async Task<BranchRegisterRequest> GetBranchLicenseStatusAsync(int branchId)
        {
            var registrationRequest = await _branchRepository.GetBranchRegisterRequestAsync(branchId);
            if (registrationRequest == null)
            {
                throw new Exception($"No registration request found for branch ID {branchId}");
            }

            return registrationRequest;
        }

        public async Task<PaginatedResponse<PendingRegistrationDto>> GetPendingBranchRegistrationsAsync(int pageNumber, int pageSize)
        {
            var (allRequests, totalCount) = await _branchRepository.GetAllBranchRegisterRequestsAsync(pageNumber, pageSize);
            var items = allRequests
                .Where(r => r.Status == RegisterVendorStatusEnum.Pending)
                .Select(r => new PendingRegistrationDto
                {
                    BranchRegisterRequestId = r.BranchRegisterRequestId,
                    BranchId = r.BranchId,
                    LicenseUrl = r.LicenseUrl,
                    Status = r.Status,
                    RejectReason = r.RejectReason,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    Branch = r.Branch == null ? null : new PendingRegistrationDto.PendingBranchInfo
                    {
                        BranchId = r.Branch.BranchId,
                        VendorId = r.Branch.VendorId,
                        ManagerId = r.Branch.ManagerId,
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
                        IsActive = r.Branch.IsActive,
                        IsSubscribed = r.Branch.IsSubscribed,
                        SubscriptionExpiresAt = r.Branch.SubscriptionExpiresAt,
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
            var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId);
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

        private async Task<BranchResponseDto> MapToResponseDtoAsync(Branch branch)
        {
            var licenseRequest = await _branchRepository.GetBranchRegisterRequestAsync(branch.BranchId);
            
            List<string> licenseUrls = new List<string>();

            if (!string.IsNullOrEmpty(licenseRequest?.LicenseUrl))
            {
                if (licenseRequest.LicenseUrl.TrimStart().StartsWith("["))
                {
                    try
                    {
                        licenseUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(licenseRequest.LicenseUrl);
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

            return new BranchResponseDto
            {
                BranchId = branch.BranchId,
                VendorId = branch.VendorId,
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
                IsActive = branch.IsActive,
                IsSubscribed = branch.IsSubscribed,
                SubscriptionExpiresAt = branch.SubscriptionExpiresAt,
                DaysRemaining = branch.SubscriptionExpiresAt.HasValue 
                    ? (int)Math.Ceiling((branch.SubscriptionExpiresAt.Value - DateTime.UtcNow).TotalDays)
                    : null,
                LicenseUrls = licenseUrls,
                LicenseStatus = licenseRequest?.Status.ToString(),
                LicenseRejectReason = licenseRequest?.RejectReason
            };
        }

        private BO.DTO.Branch.BranchPublicDto MapToPublicDto(Branch branch)
        {
            // Public version without vendor-specific fields
            return new BO.DTO.Branch.BranchPublicDto
            {
                BranchId = branch.BranchId,
                VendorId = branch.VendorId,
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
                IsActive = branch.IsActive,
            };
        }

        // ==================== WORK SCHEDULE OPERATIONS ====================

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

        // ==================== DAY OFF OPERATIONS ====================

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

        // ==================== BRANCH IMAGE OPERATIONS ====================

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

        // ==================== ACTIVE BRANCHES WITH DYNAMIC FILTERING ====================

        public async Task<ActiveBranchListResponseDto> GetActiveBranchesFilteredAsync(ActiveBranchFilterDto filter)
        {
            // Check if NO filters provided at all
            bool hasLatLong = filter.Lat.HasValue && filter.Long.HasValue;
            bool hasDistance = filter.Distance.HasValue;
            bool hasPrice = filter.MinPrice.HasValue || filter.MaxPrice.HasValue;
            bool hasTaste = filter.TasteIds != null && filter.TasteIds.Count > 0;
            bool hasDietary = filter.DietaryIds != null && filter.DietaryIds.Count > 0;
            bool hasAnyFilter = hasLatLong || hasDistance || hasPrice || hasTaste || hasDietary;

            // If NO filters provided, return all active branches without filtering
            if (!hasAnyFilter)
            {
                var allBranches = await _branchRepository.GetAllActiveBranchesWithoutFilterAsync();
                
                var allResponseDtos = allBranches.Select(branch =>
                {
                    var dishes = branch.Dishes.Where(d => d.IsActive);

                    return new ActiveBranchResponseDto
                    {
                        BranchId      = branch.BranchId,
                        VendorId      = branch.VendorId,
                        VendorName    = branch.Vendor?.Name ?? string.Empty,
                        Name          = branch.Name,
                        PhoneNumber   = branch.PhoneNumber,
                        Email         = branch.Email,
                        AddressDetail = branch.AddressDetail,
                        Ward          = branch.Ward,
                        City          = branch.City,
                        Lat           = branch.Lat,
                        Long          = branch.Long,
                        AvgRating     = branch.AvgRating,
                        IsVerified    = branch.IsVerified,
                        DistanceKm    = null, // No distance calculation when no lat/long provided
                        Dishes = dishes.Select(dish => new ActiveDishResponseDto
                        {
                            DishId       = dish.DishId,
                            Name         = dish.Name,
                            Price        = dish.Price,
                            Description  = dish.Description,
                            ImageUrl     = dish.ImageUrl,
                            IsSoldOut    = dish.IsSoldOut,
                            CategoryName = dish.Category?.Name ?? string.Empty,
                            TasteNames = dish.DishTastes?
                                .Select(dt => dt.Taste?.Name ?? string.Empty)
                                .Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new(),
                            DietaryPreferenceNames = dish.DishDietaryPreferences?
                                .Select(ddp => ddp.DietaryPreference?.Name ?? string.Empty)
                                .Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new()
                        }).ToList()
                    };
                }).ToList();

                return new ActiveBranchListResponseDto
                {
                    Items      = allResponseDtos,
                    TotalCount = allResponseDtos.Count
                };
            }

            // If filters provided, use filtered logic
            if (filter.MinPrice.HasValue && filter.MaxPrice.HasValue && filter.MinPrice > filter.MaxPrice)
                throw new Exception("MinPrice cannot be greater than MaxPrice");

            // Default coordinates: Ho Chi Minh City center (if not provided)
            double userLat = filter.Lat ?? 10.8231;  // Default: HCM latitude
            double userLong = filter.Long ?? 106.6297;  // Default: HCM longitude
            double maxDistance = filter.Distance ?? 10.0;

            // DAL handles ALL filtering logic (distance, price, taste, dietary)
            var items = await _branchRepository.GetActiveBranchesFilteredAsync(
                userLat, userLong, maxDistance,
                filter.DietaryIds, filter.TasteIds,
                filter.MinPrice, filter.MaxPrice);

            // Service layer only maps to DTOs - NO additional filtering
            var responseDtos = items.Select(item =>
            {
                var branch     = item.branch;
                var distanceKm = item.distanceKm;

                // Map all active dishes (already filtered by DAL)
                var dishes = branch.Dishes.Where(d => d.IsActive);

                return new ActiveBranchResponseDto
                {
                    BranchId      = branch.BranchId,
                    VendorId      = branch.VendorId,
                    VendorName    = branch.Vendor?.Name ?? string.Empty,
                    Name          = branch.Name,
                    PhoneNumber   = branch.PhoneNumber,
                    Email         = branch.Email,
                    AddressDetail = branch.AddressDetail,
                    Ward          = branch.Ward,
                    City          = branch.City,
                    Lat           = branch.Lat,
                    Long          = branch.Long,
                    AvgRating     = branch.AvgRating,
                    IsVerified    = branch.IsVerified,
                    DistanceKm    = Math.Round(distanceKm, 2),
                    Dishes = dishes.Select(dish => new ActiveDishResponseDto
                    {
                        DishId       = dish.DishId,
                        Name         = dish.Name,
                        Price        = dish.Price,
                        Description  = dish.Description,
                        ImageUrl     = dish.ImageUrl,
                        IsSoldOut    = dish.IsSoldOut,
                        CategoryName = dish.Category?.Name ?? string.Empty,
                        TasteNames = dish.DishTastes?
                            .Select(dt => dt.Taste?.Name ?? string.Empty)
                            .Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new(),
                        DietaryPreferenceNames = dish.DishDietaryPreferences?
                            .Select(ddp => ddp.DietaryPreference?.Name ?? string.Empty)
                            .Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new()
                    }).ToList()
                };
            }).ToList();

            return new ActiveBranchListResponseDto
            {
                Items      = responseDtos,
                TotalCount = responseDtos.Count
            };
        }
    }
}
