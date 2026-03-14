using DAL;
using Microsoft.EntityFrameworkCore;
using Service.Interfaces;

namespace Service;

public class BranchMetricsService : IBranchMetricsService
{
    private readonly StreetFoodDbContext _context;

    public BranchMetricsService(StreetFoodDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task OnFeedbackCreated(int branchId, int rating)
    {
        await _context.Database.ExecuteSqlRawAsync(
            @"UPDATE ""Branches""
              SET ""TotalReviewCount"" = ""TotalReviewCount"" + 1,
                  ""TotalRatingSum"" = ""TotalRatingSum"" + {0},
                  ""AvgRating"" = CAST((""TotalRatingSum"" + {0}) AS DOUBLE PRECISION) / (""TotalReviewCount"" + 1)
              WHERE ""BranchId"" = {1}",
            rating, branchId);
    }

    public async Task OnFeedbackUpdated(int branchId, int oldRating, int newRating)
    {
        if (oldRating == newRating) return;

        int delta = newRating - oldRating;
        await _context.Database.ExecuteSqlRawAsync(
            @"UPDATE ""Branches""
              SET ""TotalRatingSum"" = ""TotalRatingSum"" + {0},
                  ""AvgRating"" = CASE WHEN ""TotalReviewCount"" > 0
                      THEN CAST((""TotalRatingSum"" + {0}) AS DOUBLE PRECISION) / ""TotalReviewCount""
                      ELSE 0 END
              WHERE ""BranchId"" = {1}",
            delta, branchId);
    }

    public async Task OnFeedbackDeleted(int branchId, int rating)
    {
        await _context.Database.ExecuteSqlRawAsync(
            @"UPDATE ""Branches""
              SET ""TotalReviewCount"" = GREATEST(""TotalReviewCount"" - 1, 0),
                  ""TotalRatingSum"" = GREATEST(""TotalRatingSum"" - {0}, 0),
                  ""AvgRating"" = CASE WHEN ""TotalReviewCount"" - 1 > 0
                      THEN CAST((""TotalRatingSum"" - {0}) AS DOUBLE PRECISION) / (""TotalReviewCount"" - 1)
                      ELSE 0 END
              WHERE ""BranchId"" = {1}",
            rating, branchId);
    }

    public async Task RecalculateFromScratch(int branchId)
    {
        // Two-step approach: first reset to zero, then update from subquery.
        // This handles the case where a branch has zero feedbacks (subquery returns no rows).
        await _context.Database.ExecuteSqlRawAsync(
            @"UPDATE ""Branches""
              SET ""TotalReviewCount"" = 0, ""TotalRatingSum"" = 0, ""AvgRating"" = 0
              WHERE ""BranchId"" = {0}",
            branchId);

        await _context.Database.ExecuteSqlRawAsync(
            @"UPDATE ""Branches"" b
              SET ""TotalReviewCount"" = sub.cnt,
                  ""TotalRatingSum"" = sub.total,
                  ""AvgRating"" = CAST(sub.total AS DOUBLE PRECISION) / sub.cnt
              FROM (
                  SELECT ""BranchId"", COUNT(*) as cnt, SUM(""Rating"") as total
                  FROM ""Feedbacks""
                  WHERE ""BranchId"" = {0}
                  GROUP BY ""BranchId""
              ) sub
              WHERE b.""BranchId"" = {0} AND b.""BranchId"" = sub.""BranchId""",
            branchId);
    }
}
