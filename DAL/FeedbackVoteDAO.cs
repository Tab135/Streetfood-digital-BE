using BO.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL;

public class FeedbackVoteDAO
{
    private readonly StreetFoodDbContext _context;

    public FeedbackVoteDAO(StreetFoodDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<FeedbackVote?> GetByFeedbackAndUserAsync(int feedbackId, int userId)
    {
        return await _context.FeedbackVotes
            .FirstOrDefaultAsync(v => v.FeedbackId == feedbackId && v.UserId == userId);
    }

    public async Task<FeedbackVote> CreateAsync(FeedbackVote vote)
    {
        _context.FeedbackVotes.Add(vote);
        await _context.SaveChangesAsync();
        return vote;
    }

    public async Task UpdateAsync(FeedbackVote vote)
    {
        vote.UpdatedAt = DateTime.UtcNow;
        _context.FeedbackVotes.Update(vote);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(FeedbackVote vote)
    {
        _context.FeedbackVotes.Remove(vote);
        await _context.SaveChangesAsync();
    }

    public async Task<int> CountByFeedbackAndTypeAsync(int feedbackId, VoteType voteType)
    {
        return await _context.FeedbackVotes
            .CountAsync(v => v.FeedbackId == feedbackId && v.VoteType == voteType);
    }
}
