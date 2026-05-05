using System;
using System.Threading.Tasks;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service;

public class BranchMetricsService : IBranchMetricsService
{
    private const string TierFeedbackWindowSizeSettingName = "vendorTierFeedbackWindowSize";
    private const int DefaultTierFeedbackWindowSize = 20;

    private readonly IBranchRepository _branchRepository;
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly ISettingService _settingService;

    public BranchMetricsService(
        IBranchRepository branchRepository,
        IFeedbackRepository feedbackRepository,
        ISettingService settingService)
    {
        _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
        _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
        _settingService = settingService ?? throw new ArgumentNullException(nameof(settingService));
    }

    public async Task OnFeedbackCreated(int branchId, int rating)
    {
        var branch = await _branchRepository.GetByIdAsync(branchId);
        if (branch == null) return;

        int feedbackWindowSize = GetFeedbackWindowSize();

        // --- Tier Logic Processing ---
        int newBatchReviewCount = branch.BatchReviewCount;
        int newBatchRatingSum = branch.BatchRatingSum;
        int newTierId = branch.TierId;
        bool banBranch = false;

        if (newBatchReviewCount < feedbackWindowSize)
        {
            newBatchReviewCount++;
            newBatchRatingSum += rating;
        }
        else // == feedbackWindowSize
        {
            // OnFeedbackCreated runs after new feedback is saved.
            // Need to remove the (windowSize + 1)-th oldest item from rolling sum.
            var droppedRating = await _feedbackRepository.GetRatingOfRecentFeedbackAsync(branchId, feedbackWindowSize);
            if (droppedRating.HasValue)
            {
                newBatchRatingSum = newBatchRatingSum - droppedRating.Value + rating;
            }
            else
            {
                newBatchRatingSum += rating;
            }
        }

        // Re-evaluate tier only when we have enough feedback in rolling window.
        if (newBatchReviewCount >= feedbackWindowSize)
        {
            double average = (double)newBatchRatingSum / feedbackWindowSize;
            newTierId = ResolveTierIdByAverage(average);
            banBranch = newTierId == 1 && average < 2.0;
        }

        await _branchRepository.UpdateBranchMetricsAndTierAsync(
            branchId, rating, newBatchReviewCount, newBatchRatingSum, newTierId, banBranch);
    }

    public async Task OnFeedbackUpdated(int branchId, int oldRating, int newRating)
    {
        int feedbackWindowSize = GetFeedbackWindowSize();
        await _branchRepository.UpdateBranchMetricsOnFeedbackUpdatedAsync(branchId, oldRating, newRating);
        await _branchRepository.RecalculateBranchMetricsAsync(branchId, feedbackWindowSize);
    }

    public async Task OnFeedbackDeleted(int branchId, int rating)
    {
        int feedbackWindowSize = GetFeedbackWindowSize();
        await _branchRepository.UpdateBranchMetricsOnFeedbackDeletedAsync(branchId, rating);
        await _branchRepository.RecalculateBranchMetricsAsync(branchId, feedbackWindowSize);
    }

    public async Task RecalculateFromScratch(int branchId)
    {
        int feedbackWindowSize = GetFeedbackWindowSize();
        await _branchRepository.RecalculateBranchMetricsAsync(branchId, feedbackWindowSize);
    }

    private int GetFeedbackWindowSize()
    {
        int configured = _settingService.GetInt(TierFeedbackWindowSizeSettingName, DefaultTierFeedbackWindowSize);
        return configured > 0 ? configured : DefaultTierFeedbackWindowSize;
    }

    private static int ResolveTierIdByAverage(double average)
    {
        if (average >= 4.5) return 4; // Diamond
        if (average >= 3.0) return 3; // Gold
        if (average >= 2.0) return 2; // Silver
        return 1; // Warning
    }
}
