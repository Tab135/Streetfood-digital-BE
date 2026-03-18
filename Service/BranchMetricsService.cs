using System;
using System.Threading.Tasks;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service;

public class BranchMetricsService : IBranchMetricsService
{
    private readonly IBranchRepository _branchRepository;
    private readonly IFeedbackRepository _feedbackRepository;

    public BranchMetricsService(IBranchRepository branchRepository, IFeedbackRepository feedbackRepository)
    {
        _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
        _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
    }

    public async Task OnFeedbackCreated(int branchId, int rating)
    {
        var branch = await _branchRepository.GetByIdAsync(branchId);
        if (branch == null) return;

        // --- Tier Logic Processing ---
        int newBatchReviewCount = branch.BatchReviewCount;
        int newBatchRatingSum = branch.BatchRatingSum;
        int newTierId = branch.TierId;
        bool banBranch = false;

        if (newBatchReviewCount < 20)
        {
            newBatchReviewCount++;
            newBatchRatingSum += rating;
        }
        else // == 20
        {
            // Trừ đi điểm của review thứ 21 (là cái cũ nhất trong danh sách 21 cái vì chúng ta gọi Get sau khi Feedback đã tạo)
            // Wait: Khi OnFeedbackCreated chạy thì feedback LẼ RA ĐÃ ĐƯỢC TẠO rồi vào DB. 
            // Vậy 20 cái mới nhất là gồm cái rating hiện tại, và 19 cái trước đó. Cái thứ 21 sẽ là cái bị out khỏi list.
            var oldRating21 = await _feedbackRepository.GetRatingOfRecentFeedbackAsync(branchId, 20); // skip 20 = get 21st review
            if (oldRating21.HasValue)
            {
                newBatchRatingSum = newBatchRatingSum - oldRating21.Value + rating;
            }
            else
            {
                // Fallback nếu có lỗi data từ trước dẫn tới sum lệch
                newBatchRatingSum += rating;
            }
        }

        // Kiểm tra chuyển bậc Tier
        if (newBatchReviewCount >= 20)
        {
            double average = (double)newBatchRatingSum / 20;

            if (average >= 3.0)
            {
                // Tăng 1 bậc Tier (Không phân biệt đã thanh toán hay chưa)
                if (newTierId < 4) // Diamond = 4
                {
                    newTierId++;
                    // Reset lại chu kỳ sau khi lên tier không? (Requirement không đề cập đến reset, nên chúng ta giữ rolling window)
                }
            }
            else if (average <= 2.0)
            {
                // Giảm 1 bậc Tier
                if (newTierId > 1) // Warning = 1
                {
                    newTierId--;
                }
                
                // Đặc biệt: Nếu Tier == Warning VÀ Average <= 2.0 -> Set IsActive = False (Ban quán)
                if (branch.TierId == 1) // So với tier cũ trước khi trừ, hoặc newTierid cũng k sao vì nó ko thể bé hơn 1
                {
                    banBranch = true;
                }
            }
        }

        await _branchRepository.UpdateBranchMetricsAndTierAsync(
            branchId, rating, newBatchReviewCount, newBatchRatingSum, newTierId, banBranch);
    }

    public async Task OnFeedbackUpdated(int branchId, int oldRating, int newRating)
    {
        await _branchRepository.UpdateBranchMetricsOnFeedbackUpdatedAsync(branchId, oldRating, newRating);
        // Call recalculate to ensure rolling window stats are sync if the updated feedback was in the last 20
        await _branchRepository.RecalculateBranchMetricsAsync(branchId);
    }

    public async Task OnFeedbackDeleted(int branchId, int rating)
    {
        await _branchRepository.UpdateBranchMetricsOnFeedbackDeletedAsync(branchId, rating);
        // Call recalculate to sync rolling window stats properly
        await _branchRepository.RecalculateBranchMetricsAsync(branchId);
    }

    public async Task RecalculateFromScratch(int branchId)
    {
        await _branchRepository.RecalculateBranchMetricsAsync(branchId);
    }
}
