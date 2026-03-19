using BO.Common;
using BO.DTO.Branch;
using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IBranchService
    {
        // CRUD operations
        Task<BranchResponseDto> CreateUserBranchAsync(CreateUserBranchRequest request, int userId);
        Task<Branch> CreateBranchAsync(CreateBranchDto createBranchDto, int vendorId, int userId);
        Task<BranchResponseDto> GetBranchByIdAsync(int branchId);
        
        // User Branch specifics (Replacing GhostPin logic)
        Task<BranchResponseDto> ApproveUserBranchAsync(int branchId);
        Task<BranchResponseDto> RejectUserBranchAsync(int branchId, RejectUserBranchRequest request);
        Task<BranchResponseDto> AuditUserBranchAsync(int branchId, AuditUserBranchRequest request);
        Task<object> ClaimUserBranchAsync(int branchId, int vendorId, int userId, ClaimUserBranchRequest request);

        Task<PaginatedResponse<BranchResponseDto>> GetBranchesByVendorIdAsync(int vendorId, int pageNumber, int pageSize);
        Task<PaginatedResponse<BranchResponseDto>> GetAllBranchesAsync(int pageNumber, int pageSize);
        Task<PaginatedResponse<BranchResponseDto>> GetActiveBranchesAsync(int pageNumber, int pageSize);
        Task<PaginatedResponse<BranchResponseDto>> GetUnverifiedBranchesAsync(int pageNumber, int pageSize);
        Task<BranchResponseDto> UpdateBranchAsync(int branchId, UpdateBranchDto updateBranchDto, int userId);
        Task DeleteBranchAsync(int branchId, int userId);
        Task<bool> UserOwnsBranchAsync(int branchId, int userId);
        
        // License submission and verification
        Task<BranchRegisterRequest> SubmitBranchLicenseAsync(int branchId, List<string> licenseImagePaths, int userId);
        Task<BranchRegisterRequest> GetBranchLicenseStatusAsync(int branchId, int userId);
        Task<PaginatedResponse<PendingRegistrationDto>> GetPendingBranchRegistrationsAsync(int pageNumber, int pageSize);
        Task<bool> VerifyBranchAsync(int branchId);
        Task<bool> RejectBranchRegistrationAsync(int branchId, string rejectionReason);

        // Work Schedule operations
        Task<List<WorkSchedule>> AddWorkScheduleAsync(int branchId, AddWorkScheduleDto dto, int userId);
        Task<List<WorkScheduleResponseDto>> GetBranchWorkSchedulesAsync(int branchId);
        Task<WorkSchedule> UpdateWorkScheduleAsync(int scheduleId, UpdateWorkScheduleDto dto, int userId);
        Task DeleteWorkScheduleAsync(int scheduleId, int userId);

        // Day Off operations
        Task<DayOff> AddDayOffAsync(int branchId, AddDayOffDto dto, int userId);
        Task<List<DayOffResponseDto>> GetBranchDayOffsAsync(int branchId);
        Task DeleteDayOffAsync(int dayOffId, int userId);

        // Branch Image operations
        Task<BranchImage> AddBranchImageAsync(int branchId, string imageUrl, int userId);
        Task<PaginatedResponse<BranchImageResponseDto>> GetBranchImagesAsync(int branchId, int pageNumber, int pageSize);
        Task DeleteBranchImageAsync(int imageId, int userId);

        // Active branches with dynamic filtering
        Task<ActiveBranchListResponseDto> GetActiveBranchesFilteredAsync(ActiveBranchFilterDto filter);

        // Vendor ownership check
        Task<bool> IsVendorOwnedByUserAsync(int vendorId, int userId);
    }
}
