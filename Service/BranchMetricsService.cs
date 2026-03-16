using System;
using System.Threading.Tasks;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service;

public class BranchMetricsService : IBranchMetricsService
{
    private readonly IBranchRepository _branchRepository;

    public BranchMetricsService(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
    }

    public async Task OnFeedbackCreated(int branchId, int rating)
    {
        await _branchRepository.UpdateBranchMetricsOnFeedbackCreatedAsync(branchId, rating);
    }

    public async Task OnFeedbackUpdated(int branchId, int oldRating, int newRating)
    {
        await _branchRepository.UpdateBranchMetricsOnFeedbackUpdatedAsync(branchId, oldRating, newRating);
    }

    public async Task OnFeedbackDeleted(int branchId, int rating)
    {
        await _branchRepository.UpdateBranchMetricsOnFeedbackDeletedAsync(branchId, rating);
    }

    public async Task RecalculateFromScratch(int branchId)
    {
        await _branchRepository.RecalculateBranchMetricsAsync(branchId);
    }
}
