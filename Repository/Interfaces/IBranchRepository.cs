using BO.DTO.Branch;
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
        Task<(List<Branch> items, int totalCount)> GetByCreatedByIdAsync(int userId, int pageNumber, int pageSize);
        Task<(List<Branch> items, int totalCount)> GetAllApprovedGhostPinsAsync(int pageNumber, int pageSize);
        Task<(List<Branch> items, int totalCount)> GetByVendorIdAsync(int vendorId, int pageNumber, int pageSize);
        Task<List<Branch>> GetAllByManagerIdAsync(int managerUserId);
        Task<(List<Branch> items, int totalCount)> GetAllAsync(int pageNumber, int pageSize);
        Task<(List<Branch> items, int totalCount)> GetActiveBranchesAsync(int pageNumber, int pageSize);
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
        Task<BranchRequest> GetBranchRequestAsync(int branchId);
        Task<Dictionary<int, BranchRequest>> GetRegisterRequestsByBranchIdsAsync(List<int> branchIds);
        Task<(List<BranchRequest> items, int totalCount)> GetAllBranchRequestsAsync(int pageNumber, int pageSize, int? type = null);
        Task AddBranchRequestAsync(BranchRequest request);
        Task UpdateBranchRequestAsync(BranchRequest request);
        

        // Get all active branches without any filtering
        Task<List<Branch>> GetAllActiveBranchesWithoutFilterAsync();

        Task<(List<SimilarBranchResponseDto> items, int totalCount)> GetSimilarBranchesByDishesAsync(int branchId, int pageNumber, int pageSize);

        // Active branches with dynamic filtering - returns all matching results
        Task<List<(Branch branch, double distanceKm)>> GetActiveBranchesFilteredAsync(
            double? userLat,
            double? userLong,
            double? maxDistanceKm,
            List<int>? dietaryIds,
            List<int>? tasteIds,
            decimal? minPrice,
            decimal? maxPrice,
            List<int>? categoryIds);

        Task UpdateBranchMetricsAndTierAsync(int branchId, int rating, int newBatchReviewCount, int newBatchRatingSum, int newTierId, bool banBranch);
        Task UpdateBranchMetricsOnFeedbackUpdatedAsync(int branchId, int oldRating, int newRating);
        Task UpdateBranchMetricsOnFeedbackDeletedAsync(int branchId, int rating);
        Task RecalculateBranchMetricsAsync(int branchId);
    }
}


