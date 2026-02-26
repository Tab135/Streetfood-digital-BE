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

        public BranchService(
            IBranchRepository branchRepository,
            IVendorRepository vendorRepository)
        {
            _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
            _vendorRepository = vendorRepository ?? throw new ArgumentNullException(nameof(vendorRepository));
        }

        public async Task<Branch> CreateBranchAsync(CreateBranchDto createBranchDto, int vendorId, int userId)
        {
            // Verify vendor exists
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new Exception($"Vendor with ID {vendorId} not found");
            }

            // Verify user owns the vendor
            if (vendor.UserId != userId)
            {
                throw new Exception("User does not own this vendor");
            }

            var branch = new Branch
            {
                VendorId = vendorId,
                UserId = userId,
                Name = createBranchDto.Name,
                PhoneNumber = createBranchDto.PhoneNumber,
                Email = createBranchDto.Email,
                AddressDetail = createBranchDto.AddressDetail,
                BuildingName = createBranchDto.BuildingName,
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

            return MapToResponseDto(branch);
        }

        public async Task<PaginatedResponse<BranchResponseDto>> GetBranchesByVendorIdAsync(int vendorId, int pageNumber, int pageSize)
        {
            var (branches, totalCount) = await _branchRepository.GetByVendorIdAsync(vendorId, pageNumber, pageSize);
            var items = branches.Select(MapToResponseDto).ToList();
            return new PaginatedResponse<BranchResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PaginatedResponse<BranchResponseDto>> GetAllBranchesAsync(int pageNumber, int pageSize)
        {
            var (branches, totalCount) = await _branchRepository.GetAllAsync(pageNumber, pageSize);
            var items = branches.Select(MapToResponseDto).ToList();
            return new PaginatedResponse<BranchResponseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PaginatedResponse<BranchResponseDto>> GetActiveBranchesAsync(int pageNumber, int pageSize)
        {
            var (branches, totalCount) = await _branchRepository.GetActiveBranchesAsync(pageNumber, pageSize);
            var items = branches.Select(MapToResponseDto).ToList();
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

            if (!string.IsNullOrEmpty(updateBranchDto.BuildingName))
                branch.BuildingName = updateBranchDto.BuildingName;

            if (!string.IsNullOrEmpty(updateBranchDto.Ward))
                branch.Ward = updateBranchDto.Ward;

            if (!string.IsNullOrEmpty(updateBranchDto.City))
                branch.City = updateBranchDto.City;

            if (updateBranchDto.Lat.HasValue)
                branch.Lat = updateBranchDto.Lat.Value;

            if (updateBranchDto.Long.HasValue)
                branch.Long = updateBranchDto.Long.Value;

            if (updateBranchDto.IsActive.HasValue)
                branch.IsActive = updateBranchDto.IsActive.Value;

            await _branchRepository.UpdateAsync(branch);

            return MapToResponseDto(branch);
        }

        public async Task DeleteBranchAsync(int branchId, int userId)
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
            return branches.Select(MapToResponseDtoAsync).Select(t => t.Result).ToList();
        }

        public async Task<PaginatedResponse<BranchResponseDto>> GetUnverifiedBranchesAsync(int pageNumber, int pageSize)
        {
            var (branches, totalCount) = await _branchRepository.GetUnverifiedBranchesAsync(pageNumber, pageSize);
            var items = branches.Select(MapToResponseDtoAsync).Select(t => t.Result).ToList();
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

        public async Task<PaginatedResponse<BranchRegisterRequest>> GetPendingBranchRegistrationsAsync(int pageNumber, int pageSize)
        {
            var (allRequests, totalCount) = await _branchRepository.GetAllBranchRegisterRequestsAsync(pageNumber, pageSize);
            var items = allRequests.Where(r => r.Status == RegisterVendorStatusEnum.Pending).ToList();
            return new PaginatedResponse<BranchRegisterRequest>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<bool> VerifyBranchAsync(int branchId)
        {
            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch == null)
            {
                throw new Exception($"Branch with ID {branchId} not found");
            }

            branch.IsVerified = true;
            branch.IsActive = true; // Activate branch when verified
            await _branchRepository.UpdateAsync(branch);

            // Update registration request status
            var registrationRequest = await _branchRepository.GetBranchRegisterRequestAsync(branchId);
            if (registrationRequest != null)
            {
                registrationRequest.Status = RegisterVendorStatusEnum.Accept;
                registrationRequest.UpdatedAt = DateTime.UtcNow;
                await _branchRepository.UpdateBranchRegisterRequestAsync(registrationRequest);
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
            string firstLicenseUrl = licenseRequest?.LicenseUrl;

            if (!string.IsNullOrEmpty(licenseRequest?.LicenseUrl))
            {
                if (licenseRequest.LicenseUrl.TrimStart().StartsWith("["))
                {
                    try
                    {
                        licenseUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(licenseRequest.LicenseUrl);
                        firstLicenseUrl = licenseUrls?.FirstOrDefault();
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
                UserId = branch.UserId,
                Name = branch.Name,
                PhoneNumber = branch.PhoneNumber,
                Email = branch.Email,
                AddressDetail = branch.AddressDetail,
                BuildingName = branch.BuildingName,
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
                LicenseUrl = firstLicenseUrl,
                LicenseUrls = licenseUrls,
                LicenseStatus = licenseRequest?.Status.ToString(),
                LicenseRejectReason = licenseRequest?.RejectReason
            };
        }

        private BranchResponseDto MapToResponseDto(Branch branch)
        {
            // Sync version - license info fetched separately if needed
            return new BranchResponseDto
            {
                BranchId = branch.BranchId,
                VendorId = branch.VendorId,
                UserId = branch.UserId,
                Name = branch.Name,
                PhoneNumber = branch.PhoneNumber,
                Email = branch.Email,
                AddressDetail = branch.AddressDetail,
                BuildingName = branch.BuildingName,
                Ward = branch.Ward,
                City = branch.City,
                Lat = branch.Lat,
                Long = branch.Long,
                CreatedAt = branch.CreatedAt,
                UpdatedAt = branch.UpdatedAt,
                IsVerified = branch.IsVerified,
                AvgRating = branch.AvgRating,
                IsActive = branch.IsActive,
                IsSubscribed = branch.IsSubscribed
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
                BranchId = i.BranchId,
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
    }
}
