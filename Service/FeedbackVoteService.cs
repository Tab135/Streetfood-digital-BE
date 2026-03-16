using BO.DTO.Feedback;
using BO.Entities;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service;

public class FeedbackVoteService : IFeedbackVoteService
{
    private readonly IFeedbackVoteRepository _voteRepository;
    private readonly IFeedbackRepository _feedbackRepository;

    public FeedbackVoteService(
        IFeedbackVoteRepository voteRepository,
        IFeedbackRepository feedbackRepository)
    {
        _voteRepository = voteRepository ?? throw new ArgumentNullException(nameof(voteRepository));
        _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
    }

    public async Task<VoteResponseDto> Vote(int feedbackId, string voteType, int userId)
    {
        // Parse vote type
        if (voteType != "up" && voteType != "down")
            throw new Exception("VoteType must be 'up' or 'down'");

        var parsedVoteType = voteType == "up" ? VoteType.Up : VoteType.Down;

        // Validate feedback exists
        var feedback = await _feedbackRepository.GetById(feedbackId);
        if (feedback == null)
            throw new Exception("Feedback not found");

        // Cannot vote on own feedback
        if (feedback.UserId == userId)
            throw new Exception("You cannot vote on your own review");

        // Must have visited branch (has existing feedback on the same branch)
        var hasVisited = await _feedbackRepository.HasUserFeedbackOnBranch(feedback.BranchId, userId);
        if (!hasVisited)
            throw new UnauthorizedAccessException("You must have reviewed this branch to vote on reviews");

        // Check for existing vote
        var existingVote = await _voteRepository.GetByFeedbackAndUser(feedbackId, userId);

        if (existingVote == null)
        {
            // Create new vote
            await _voteRepository.Create(new FeedbackVote
            {
                FeedbackId = feedbackId,
                UserId = userId,
                VoteType = parsedVoteType,
                CreatedAt = DateTime.UtcNow
            });
        }
        else if (existingVote.VoteType == parsedVoteType)
        {
            // Same direction → toggle off (remove)
            await _voteRepository.Delete(existingVote);
        }
        else
        {
            // Different direction → change
            existingVote.VoteType = parsedVoteType;
            await _voteRepository.Update(existingVote);
        }

        // Return updated counts
        return await GetVoteCounts(feedbackId, userId);
    }

    private async Task<VoteResponseDto> GetVoteCounts(int feedbackId, int userId)
    {
        var upVotes = await _voteRepository.CountByFeedbackAndType(feedbackId, VoteType.Up);
        var downVotes = await _voteRepository.CountByFeedbackAndType(feedbackId, VoteType.Down);
        var userVote = await _voteRepository.GetByFeedbackAndUser(feedbackId, userId);

        return new VoteResponseDto
        {
            UpVotes = upVotes,
            DownVotes = downVotes,
            NetScore = upVotes - downVotes,
            UserVote = userVote?.VoteType == VoteType.Up ? "up" :
                       userVote?.VoteType == VoteType.Down ? "down" : null
        };
    }
}
