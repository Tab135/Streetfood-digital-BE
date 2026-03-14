using BO.Entities;
using DAL;
using Repository.Interfaces;

namespace Repository;

public class FeedbackVoteRepository : IFeedbackVoteRepository
{
    private readonly FeedbackVoteDAO _feedbackVoteDAO;

    public FeedbackVoteRepository(FeedbackVoteDAO feedbackVoteDAO)
    {
        _feedbackVoteDAO = feedbackVoteDAO ?? throw new ArgumentNullException(nameof(feedbackVoteDAO));
    }

    public async Task<FeedbackVote?> GetByFeedbackAndUser(int feedbackId, int userId) =>
        await _feedbackVoteDAO.GetByFeedbackAndUserAsync(feedbackId, userId);

    public async Task<FeedbackVote> Create(FeedbackVote vote) =>
        await _feedbackVoteDAO.CreateAsync(vote);

    public async Task Update(FeedbackVote vote) =>
        await _feedbackVoteDAO.UpdateAsync(vote);

    public async Task Delete(FeedbackVote vote) =>
        await _feedbackVoteDAO.DeleteAsync(vote);

    public async Task<int> CountByFeedbackAndType(int feedbackId, VoteType voteType) =>
        await _feedbackVoteDAO.CountByFeedbackAndTypeAsync(feedbackId, voteType);
}
