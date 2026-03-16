using BO.Entities;

namespace Repository.Interfaces;

public interface IFeedbackVoteRepository
{
    Task<FeedbackVote?> GetByFeedbackAndUser(int feedbackId, int userId);
    Task<FeedbackVote> Create(FeedbackVote vote);
    Task Update(FeedbackVote vote);
    Task Delete(FeedbackVote vote);
    Task<int> CountByFeedbackAndType(int feedbackId, VoteType voteType);
}
