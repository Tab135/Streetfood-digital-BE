using BO.DTO.Feedback;

namespace Service.Interfaces;

public interface IFeedbackVoteService
{
    Task<VoteResponseDto> Vote(int feedbackId, string voteType, int userId);
}
