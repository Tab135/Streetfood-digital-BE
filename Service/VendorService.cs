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

        public VendorService(
            IVendorRepository vendorRepository,
            IUserRepository userRepository)
        {
            _vendorRepository = vendorRepository ?? throw new ArgumentNullException(nameof(vendorRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
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
                PhoneNumber = createVendorDto.PhoneNumber,
                Email = createVendorDto.Email,
                AddressDetail = createVendorDto.AddressDetail,
                BuildingName = createVendorDto.BuildingName,
                Ward = createVendorDto.Ward,
                City = createVendorDto.City,
                Lat = createVendorDto.Lat,
                Long = createVendorDto.Long,
                IsVerified = false, // Business rule: default to false
                IsActive = true,
                IsSubscribed = false,
                AvgRating = 0
            };

            return await _vendorRepository.CreateAsync(vendor);
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

        public async Task<List<VendorResponseDto>> GetAllVendorsAsync()
        {
            var vendors = await _vendorRepository.GetAllAsync();
            return vendors.Select(MapToResponseDto).ToList();
        }

        public async Task<List<VendorResponseDto>> GetActiveVendorsAsync()
        {
            var vendors = await _vendorRepository.GetActiveVendorsAsync();
            return vendors.Select(MapToResponseDto).ToList();
        }

        public async Task<List<VendorResponseDto>> GetUnverifiedVendorsAsync()
        {
            var vendors = await _vendorRepository.GetByVerificationStatusAsync(false);
            return vendors.Select(MapToResponseDto).ToList();
        }

        public async Task<Vendor> UpdateVendorAsync(int vendorId, UpdateVendorDto updateVendorDto)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new Exception($"Vendor with ID {vendorId} not found");
            }

            vendor.Name = updateVendorDto.Name ?? vendor.Name;
            vendor.PhoneNumber = updateVendorDto.PhoneNumber ?? vendor.PhoneNumber;
            vendor.Email = updateVendorDto.Email ?? vendor.Email;
            vendor.AddressDetail = updateVendorDto.AddressDetail ?? vendor.AddressDetail;
            vendor.BuildingName = updateVendorDto.BuildingName ?? vendor.BuildingName;
            vendor.Ward = updateVendorDto.Ward ?? vendor.Ward;
            vendor.City = updateVendorDto.City ?? vendor.City;

            if (updateVendorDto.Lat.HasValue)
                vendor.Lat = updateVendorDto.Lat.Value;

            if (updateVendorDto.Long.HasValue)
                vendor.Long = updateVendorDto.Long.Value;

            await _vendorRepository.UpdateAsync(vendor);
            return vendor;
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

        public async Task<VendorRegisterRequest> SubmitVendorRegistrationAsync(int vendorId, string licenseImagePath)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new Exception($"Vendor with ID {vendorId} not found");
            }

            // Check if registration request already exists
            var existingRequest = await _vendorRepository.GetVendorRegisterRequestAsync(vendorId);

            var registrationRequest = new VendorRegisterRequest
            {
                VendorId = vendorId,
                LicenseUrl = licenseImagePath,
                Status = RegisterVendorStatusEnum.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            };

            if (existingRequest != null)
            {
                // Update existing request
                registrationRequest.VendorRegisterRequestId = existingRequest.VendorRegisterRequestId;
                await _vendorRepository.UpdateVendorRegisterRequestAsync(registrationRequest);
            }
            else
            {
                // Create new request
                await _vendorRepository.AddVendorRegisterRequestAsync(registrationRequest);
            }

            return registrationRequest;
        }

        public async Task<VendorRegisterRequest> GetVendorRegistrationStatusAsync(int vendorId)
        {
            var registrationRequest = await _vendorRepository.GetVendorRegisterRequestAsync(vendorId);
            if (registrationRequest == null)
            {
                throw new Exception($"No registration request found for vendor ID {vendorId}");
            }

            return registrationRequest;
        }

        public async Task<List<VendorRegisterRequest>> GetPendingVendorRegistrationsAsync()
        {
            // Get all pending registration requests
            var allRequests = await _vendorRepository.GetAllVendorRegisterRequestsAsync();
            return allRequests.Where(r => r.Status == RegisterVendorStatusEnum.Pending).ToList();
        }

        public async Task AddWorkScheduleAsync(int vendorId, AddWorkScheduleDto scheduleDto)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new Exception($"Vendor with ID {vendorId} not found");
            }

            var workSchedule = new WorkSchedule
            {
                VendorId = vendorId,
                Weekday = scheduleDto.Weekday,
                OpenTime = scheduleDto.OpenTime,
                CloseTime = scheduleDto.CloseTime
            };

            await _vendorRepository.AddWorkScheduleAsync(workSchedule);
        }

        public async Task AddDayOffAsync(int vendorId, AddDayOffDto dayOffDto)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new Exception($"Vendor with ID {vendorId} not found");
            }

            var dayOff = new DayOff
            {
                VendorId = vendorId,
                StartDate = dayOffDto.StartDate,
                EndDate = dayOffDto.EndDate,
                StartTime = dayOffDto.StartTime,
                EndTime = dayOffDto.EndTime
            };

            await _vendorRepository.AddDayOffAsync(dayOff);
        }

        public async Task<List<WorkSchedule>> GetVendorSchedulesAsync(int vendorId)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new Exception($"Vendor with ID {vendorId} not found");
            }

            return await _vendorRepository.GetWorkSchedulesAsync(vendorId);
        }

        public async Task<List<DayOff>> GetVendorDayOffsAsync(int vendorId)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new Exception($"Vendor with ID {vendorId} not found");
            }

            return await _vendorRepository.GetDayOffsAsync(vendorId);
        }

        public async Task AddVendorImageAsync(int vendorId, string imageUrl)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new Exception($"Vendor with ID {vendorId} not found");
            }

            var vendorImage = new VendorImage
            {
                VendorId = vendorId,
                ImageUrl = imageUrl
            };

            await _vendorRepository.AddVendorImageAsync(vendorImage);
        }

        public async Task<List<VendorImage>> GetVendorImagesAsync(int vendorId)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new Exception($"Vendor with ID {vendorId} not found");
            }

            return await _vendorRepository.GetVendorImagesAsync(vendorId);
        }

        public async Task<bool> VerifyVendorAsync(int vendorId)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new Exception($"Vendor with ID {vendorId} not found");
            }

            vendor.IsVerified = true;
            await _vendorRepository.UpdateAsync(vendor);

            // Update registration request status
            var registrationRequest = await _vendorRepository.GetVendorRegisterRequestAsync(vendorId);
            if (registrationRequest != null)
            {
                registrationRequest.Status = RegisterVendorStatusEnum.Accept;
                registrationRequest.UpdateAt = DateTime.UtcNow;
                await _vendorRepository.UpdateVendorRegisterRequestAsync(registrationRequest);
            }

            return true;
        }

        public async Task<bool> RejectVendorRegistrationAsync(int vendorId, string rejectionReason)
        {
            var registrationRequest = await _vendorRepository.GetVendorRegisterRequestAsync(vendorId);
            if (registrationRequest == null)
            {
                throw new Exception($"No registration request found for vendor ID {vendorId}");
            }

            registrationRequest.Status = RegisterVendorStatusEnum.Reject;
            registrationRequest.rejectReason = rejectionReason;
            registrationRequest.UpdateAt = DateTime.UtcNow;
            await _vendorRepository.UpdateVendorRegisterRequestAsync(registrationRequest);

            return true;
        }

        public async Task<bool> SuspendVendorAsync(int vendorId)
        {
            var vendor = await _vendorRepository.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                throw new Exception($"Vendor with ID {vendorId} not found");
            }

            vendor.IsActive = false;
            await _vendorRepository.UpdateAsync(vendor);
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
            await _vendorRepository.UpdateAsync(vendor);
            return true;
        }

        private VendorResponseDto MapToResponseDto(Vendor vendor)
        {
            return new VendorResponseDto
            {
                VendorId = vendor.VendorId,
                UserId = vendor.UserId,
                Name = vendor.Name,
                PhoneNumber = vendor.PhoneNumber,
                Email = vendor.Email,
                AddressDetail = vendor.AddressDetail,
                BuildingName = vendor.BuildingName,
                Ward = vendor.Ward,
                City = vendor.City,
                Lat = vendor.Lat,
                Long = vendor.Long,
                CreatedAt = vendor.CreatedAt,
                UpdatedAt = vendor.UpdatedAt,
                IsVerified = vendor.IsVerified,
                AvgRating = vendor.AvgRating,
                IsActive = vendor.IsActive,
                IsSubscribed = vendor.IsSubscribed,
                VendorOwnerName = vendor.VendorOwner != null ? $"{vendor.VendorOwner.FirstName} {vendor.VendorOwner.LastName}".Trim() : ""
            };
        }
    }
}
