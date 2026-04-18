using BO.DTO.Branch;
using BO.Entities;
using DAL;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class BranchRepository : IBranchRepository
    {
        private readonly BranchDAO _branchDAO;

        public BranchRepository(BranchDAO branchDAO)
        {
            _branchDAO = branchDAO ?? throw new ArgumentNullException(nameof(branchDAO));
        }

        public async Task<Branch> CreateAsync(Branch branch)
        {
            return await _branchDAO.CreateAsync(branch);
        }

        public async Task<Branch> GetByIdAsync(int branchId)
        {
            return await _branchDAO.GetByIdAsync(branchId);
        }


        public async Task<List<Branch>> GetAllByVendorIdAsync(int vendorId)
        {
            return await _branchDAO.GetAllByVendorIdAsync(vendorId);
        }

        public async Task<(List<Branch> items, int totalCount)> GetByCreatedByIdAsync(int userId, int pageNumber, int pageSize)
        {
            return await _branchDAO.GetByCreatedByIdAsync(userId, pageNumber, pageSize);
        }

        public async Task<(List<Branch> items, int totalCount)> GetAllApprovedGhostPinsAsync(int pageNumber, int pageSize)
        {
            return await _branchDAO.GetAllApprovedGhostPinsAsync(pageNumber, pageSize);
        }

        public async Task<(List<Branch> items, int totalCount)> GetByVendorIdAsync(int vendorId, int pageNumber, int pageSize, bool activeOnly = false)
        {
            return await _branchDAO.GetByVendorIdAsync(vendorId, pageNumber, pageSize, activeOnly);
        }

        public async Task<List<Branch>> GetAllByManagerIdAsync(int managerUserId)
        {
            return await _branchDAO.GetAllByManagerIdAsync(managerUserId);
        }

        public async Task<(List<Branch> items, int totalCount)> GetAllAsync(int pageNumber, int pageSize)
        {
            return await _branchDAO.GetAllAsync(pageNumber, pageSize);
        }

        public async Task<(List<Branch> items, int totalCount)> GetActiveBranchesAsync(int pageNumber, int pageSize)
        {
            return await _branchDAO.GetActiveBranchesAsync(pageNumber, pageSize);
        }


        public async Task<(List<Branch> items, int totalCount)> GetUnverifiedBranchesAsync(int pageNumber, int pageSize)
        {
            return await _branchDAO.GetUnverifiedBranchesAsync(pageNumber, pageSize);
        }

        public async Task UpdateAsync(Branch branch)
        {
            await _branchDAO.UpdateAsync(branch);
        }

        public async Task DeleteAsync(int branchId)
        {
            await _branchDAO.DeleteAsync(branchId);
        }

        public async Task<bool> ExistsByIdAsync(int branchId)
        {
            return await _branchDAO.ExistsByIdAsync(branchId);
        }

        public async Task<List<WorkSchedule>> GetWorkSchedulesAsync(int branchId)
        {
            return await _branchDAO.GetWorkSchedulesAsync(branchId);
        }

        public async Task<List<DayOff>> GetDayOffsAsync(int branchId)
        {
            return await _branchDAO.GetDayOffsAsync(branchId);
        }

        public async Task<(List<BranchImage> items, int totalCount)> GetBranchImagesAsync(int branchId, int pageNumber, int pageSize)
        {
            return await _branchDAO.GetBranchImagesAsync(branchId, pageNumber, pageSize);
        }

        public async Task AddWorkScheduleAsync(WorkSchedule workSchedule)
        {
            await _branchDAO.AddWorkScheduleAsync(workSchedule);
        }

        public async Task<WorkSchedule> GetWorkScheduleByIdAsync(int scheduleId)
        {
            return await _branchDAO.GetWorkScheduleByIdAsync(scheduleId);
        }

        public async Task UpdateWorkScheduleAsync(WorkSchedule workSchedule)
        {
            await _branchDAO.UpdateWorkScheduleAsync(workSchedule);
        }

        public async Task DeleteWorkScheduleAsync(int scheduleId)
        {
            await _branchDAO.DeleteWorkScheduleAsync(scheduleId);
        }

        public async Task AddDayOffAsync(DayOff dayOff)
        {
            await _branchDAO.AddDayOffAsync(dayOff);
        }

        public async Task<DayOff> GetDayOffByIdAsync(int dayOffId)
        {
            return await _branchDAO.GetDayOffByIdAsync(dayOffId);
        }

        public async Task DeleteDayOffAsync(int dayOffId)
        {
            await _branchDAO.DeleteDayOffAsync(dayOffId);
        }

        public async Task AddBranchImageAsync(BranchImage branchImage)
        {
            await _branchDAO.AddBranchImageAsync(branchImage);
        }

        public async Task<BranchImage> GetBranchImageByIdAsync(int imageId)
        {
            return await _branchDAO.GetBranchImageByIdAsync(imageId);
        }

        public async Task DeleteBranchImageAsync(int imageId)
        {
            await _branchDAO.DeleteBranchImageAsync(imageId);
        }

        public async Task<BranchRequest> GetBranchRequestAsync(int branchId)
        {
            return await _branchDAO.GetBranchRequestAsync(branchId);
        }

        public async Task<Dictionary<int, BranchRequest>> GetRegisterRequestsByBranchIdsAsync(List<int> branchIds)
        {
            return await _branchDAO.GetRegisterRequestsByBranchIdsAsync(branchIds);
        }

        public async Task<(List<BranchRequest> items, int totalCount)> GetAllBranchRequestsAsync(int pageNumber, int pageSize, int? type = null)
        {
            return await _branchDAO.GetAllBranchRequestsAsync(pageNumber, pageSize, type);
        }

        public async Task AddBranchRequestAsync(BranchRequest request)
        {
            await _branchDAO.AddBranchRequestAsync(request);
        }

        public async Task UpdateBranchRequestAsync(BranchRequest request)
        {
            await _branchDAO.UpdateBranchRequestAsync(request);
        }


        public async Task<List<Branch>> GetAllActiveBranchesWithoutFilterAsync()
        {
            return await _branchDAO.GetAllActiveBranchesWithoutFilterAsync();
        }

        public async Task<(List<SimilarBranchResponseDto> items, int totalCount)> GetSimilarBranchesByDishesAsync(int branchId, int pageNumber, int pageSize)
        {
            return await _branchDAO.GetSimilarBranchesByDishesAsync(branchId, pageNumber, pageSize);
        }

        public async Task<List<(Branch branch, double distanceKm)>> GetActiveBranchesFilteredAsync(
            double? userLat,
            double? userLong,
            double? maxDistanceKm,
            List<int>? dietaryIds,
            List<int>? tasteIds,
            decimal? minPrice,
            decimal? maxPrice,
            List<int>? categoryIds)
        {
            return await _branchDAO.GetActiveBranchesFilteredAsync(
                userLat, userLong, maxDistanceKm,
                dietaryIds, tasteIds,
                minPrice, maxPrice,
                categoryIds);
        }

        public async Task UpdateBranchMetricsAndTierAsync(int branchId, int rating, int newBatchReviewCount, int newBatchRatingSum, int newTierId, bool banBranch)
        {
            await _branchDAO.UpdateBranchMetricsAndTierAsync(branchId, rating, newBatchReviewCount, newBatchRatingSum, newTierId, banBranch);
        }

        public async Task UpdateBranchMetricsOnFeedbackUpdatedAsync(int branchId, int oldRating, int newRating)
        {
            await _branchDAO.UpdateBranchMetricsOnFeedbackUpdatedAsync(branchId, oldRating, newRating);
        }

        public async Task UpdateBranchMetricsOnFeedbackDeletedAsync(int branchId, int rating)
        {
            await _branchDAO.UpdateBranchMetricsOnFeedbackDeletedAsync(branchId, rating);
        }

        public async Task RecalculateBranchMetricsAsync(int branchId)
        {
            await _branchDAO.RecalculateBranchMetricsAsync(branchId);
        }

        public async Task ResetAllTiersAsync(System.Threading.CancellationToken ct)
        {
            await _branchDAO.ResetAllTiersAsync(ct);
        }
    }
}


