using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IBranchRepository
    {
        Task<Branch> CreateAsync(Branch branch);
        Task<Branch> GetByIdAsync(int branchId);
        Task<List<Branch>> GetAllByVendorIdAsync(int vendorId);  // Non-paginated for internal use
        Task<(List<Branch> items, int totalCount)> GetByVendorIdAsync(int vendorId, int pageNumber, int pageSize);
        Task<(List<Branch> items, int totalCount)> GetAllAsync(int pageNumber, int pageSize);
        Task<(List<Branch> items, int totalCount)> GetActiveBranchesAsync(int pageNumber, int pageSize);
        Task<List<Branch>> GetByVerificationStatusAsync(bool isVerified);
        Task<(List<Branch> items, int totalCount)> GetUnverifiedBranchesAsync(int pageNumber, int pageSize);
        Task UpdateAsync(Branch branch);
        Task DeleteAsync(int branchId);
        Task<bool> ExistsByIdAsync(int branchId);
        Task<List<WorkSchedule>> GetWorkSchedulesAsync(int branchId);
        Task<List<DayOff>> GetDayOffsAsync(int branchId);
        Task<(List<BranchImage> items, int totalCount)> GetBranchImagesAsync(int branchId, int pageNumber, int pageSize);
        Task AddWorkScheduleAsync(WorkSchedule workSchedule);
        Task<WorkSchedule> GetWorkScheduleByIdAsync(int scheduleId);
        Task UpdateWorkScheduleAsync(WorkSchedule workSchedule);
        Task DeleteWorkScheduleAsync(int scheduleId);
        Task AddDayOffAsync(DayOff dayOff);
        Task<DayOff> GetDayOffByIdAsync(int dayOffId);
        Task DeleteDayOffAsync(int dayOffId);
        Task AddBranchImageAsync(BranchImage branchImage);
        Task<BranchImage> GetBranchImageByIdAsync(int imageId);
        Task DeleteBranchImageAsync(int imageId);
        
        // License registration
        Task<BranchRegisterRequest> GetBranchRegisterRequestAsync(int branchId);
        Task<(List<BranchRegisterRequest> items, int totalCount)> GetAllBranchRegisterRequestsAsync(int pageNumber, int pageSize);
        Task AddBranchRegisterRequestAsync(BranchRegisterRequest request);
        Task UpdateBranchRegisterRequestAsync(BranchRegisterRequest request);
    }
}
