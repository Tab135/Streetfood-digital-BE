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

        public async Task<(List<Branch> items, int totalCount)> GetByVendorIdAsync(int vendorId, int pageNumber, int pageSize)
        {
            return await _branchDAO.GetByVendorIdAsync(vendorId, pageNumber, pageSize);
        }

        public async Task<(List<Branch> items, int totalCount)> GetAllAsync(int pageNumber, int pageSize)
        {
            return await _branchDAO.GetAllAsync(pageNumber, pageSize);
        }

        public async Task<(List<Branch> items, int totalCount)> GetActiveBranchesAsync(int pageNumber, int pageSize)
        {
            return await _branchDAO.GetActiveBranchesAsync(pageNumber, pageSize);
        }

        public async Task<List<Branch>> GetByVerificationStatusAsync(bool isVerified)
        {
            return await _branchDAO.GetByVerificationStatusAsync(isVerified);
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

        public async Task<BranchRegisterRequest> GetBranchRegisterRequestAsync(int branchId)
        {
            return await _branchDAO.GetBranchRegisterRequestAsync(branchId);
        }

        public async Task<(List<BranchRegisterRequest> items, int totalCount)> GetAllBranchRegisterRequestsAsync(int pageNumber, int pageSize)
        {
            return await _branchDAO.GetAllBranchRegisterRequestsAsync(pageNumber, pageSize);
        }

        public async Task AddBranchRegisterRequestAsync(BranchRegisterRequest request)
        {
            await _branchDAO.AddBranchRegisterRequestAsync(request);
        }

        public async Task UpdateBranchRegisterRequestAsync(BranchRegisterRequest request)
        {
            await _branchDAO.UpdateBranchRegisterRequestAsync(request);
        }

        public async Task<List<Branch>> SearchVendorsWithBranchesAndDishesAsync(string keyword)
        {
            return await _branchDAO.SearchVendorsWithBranchesAndDishesAsync(keyword);
        }

        public async Task<List<Branch>> GetAllActiveBranchesWithoutFilterAsync()
        {
            return await _branchDAO.GetAllActiveBranchesWithoutFilterAsync();
        }

        public async Task<List<(Branch branch, double distanceKm)>> GetActiveBranchesFilteredAsync(
            double userLat,
            double userLong,
            double maxDistanceKm,
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
    }
}
