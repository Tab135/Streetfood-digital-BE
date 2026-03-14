namespace Service.Interfaces;

public interface IBranchMetricsService
{
    Task OnFeedbackCreated(int branchId, int rating);
    Task OnFeedbackUpdated(int branchId, int oldRating, int newRating);
    Task OnFeedbackDeleted(int branchId, int rating);
    Task RecalculateFromScratch(int branchId);
}
