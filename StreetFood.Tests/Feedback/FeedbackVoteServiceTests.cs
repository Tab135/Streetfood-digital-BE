using System.Threading.Tasks;
using BO.DTO.Feedback;
using BO.Entities;
using BO.Exceptions;
using Moq;
using Repository.Interfaces;
using Service;
using Xunit;

namespace StreetFood.Tests.FeedbackVoteTests
{
    public class FeedbackVoteServiceTests
    {
        private readonly Mock<IFeedbackVoteRepository> _voteRepoMock;
        private readonly Mock<IFeedbackRepository> _feedbackRepoMock;
        private readonly FeedbackVoteService _voteService;

        public FeedbackVoteServiceTests()
        {
            _voteRepoMock = new Mock<IFeedbackVoteRepository>();
            _feedbackRepoMock = new Mock<IFeedbackRepository>();
            _voteService = new FeedbackVoteService(_voteRepoMock.Object, _feedbackRepoMock.Object);
        }

        private void SetupCountMocks(int feedbackId, int upVotes, int downVotes)
        {
            _voteRepoMock.Setup(r => r.CountByFeedbackAndType(feedbackId, VoteType.Up)).ReturnsAsync(upVotes);
            _voteRepoMock.Setup(r => r.CountByFeedbackAndType(feedbackId, VoteType.Down)).ReturnsAsync(downVotes);
        }

        // --- SECTION: VOTE ON FEEDBACK (SV_FVOTE_01) ---

        // UTCID01: New upvote created when user has never voted before
        [Fact]
        public async Task Vote_NewUpvote_CreatesVote()
        {
            var feedbackId = 1;
            var userId = 10;
            var feedback = new BO.Entities.Feedback { FeedbackId = feedbackId, UserId = 99 }; // different user owns it

            _feedbackRepoMock.Setup(r => r.GetById(feedbackId)).ReturnsAsync(feedback);
            _voteRepoMock.Setup(r => r.GetByFeedbackAndUser(feedbackId, userId)).ReturnsAsync((FeedbackVote?)null);
            _voteRepoMock.Setup(r => r.Create(It.IsAny<FeedbackVote>())).ReturnsAsync(new FeedbackVote());
            _voteRepoMock.Setup(r => r.GetByFeedbackAndUser(feedbackId, userId)).ReturnsAsync(new FeedbackVote { VoteType = VoteType.Up });
            SetupCountMocks(feedbackId, 1, 0);

            var result = await _voteService.Vote(feedbackId, "up", userId);

            Assert.Equal(1, result.UpVotes);
            Assert.Equal(0, result.DownVotes);
            Assert.Equal("up", result.UserVote);
        }

        // UTCID02: Same vote type again → toggles off (removes existing vote)
        [Fact]
        public async Task Vote_SameVoteType_TogglesOff()
        {
            var feedbackId = 2;
            var userId = 20;
            var feedback = new BO.Entities.Feedback { FeedbackId = feedbackId, UserId = 99 };
            var existingVote = new FeedbackVote { FeedbackId = feedbackId, UserId = userId, VoteType = VoteType.Up };

            _feedbackRepoMock.Setup(r => r.GetById(feedbackId)).ReturnsAsync(feedback);
            // First call returns existing vote, second call (in GetVoteCounts) returns null (removed)
            _voteRepoMock.SetupSequence(r => r.GetByFeedbackAndUser(feedbackId, userId))
                .ReturnsAsync(existingVote)
                .ReturnsAsync((FeedbackVote?)null);
            _voteRepoMock.Setup(r => r.Delete(existingVote)).Returns(Task.CompletedTask);
            SetupCountMocks(feedbackId, 0, 0);

            var result = await _voteService.Vote(feedbackId, "up", userId);

            _voteRepoMock.Verify(r => r.Delete(existingVote), Times.Once);
            Assert.Null(result.UserVote); // vote removed
        }

        // UTCID03: Different vote type → changes direction (up→down)
        [Fact]
        public async Task Vote_DifferentVoteType_ChangesDirection()
        {
            var feedbackId = 3;
            var userId = 30;
            var feedback = new BO.Entities.Feedback { FeedbackId = feedbackId, UserId = 99 };
            var existingVote = new FeedbackVote { FeedbackId = feedbackId, UserId = userId, VoteType = VoteType.Up };

            _feedbackRepoMock.Setup(r => r.GetById(feedbackId)).ReturnsAsync(feedback);
            _voteRepoMock.SetupSequence(r => r.GetByFeedbackAndUser(feedbackId, userId))
                .ReturnsAsync(existingVote)
                .ReturnsAsync(new FeedbackVote { VoteType = VoteType.Down });
            _voteRepoMock.Setup(r => r.Update(existingVote)).Returns(Task.CompletedTask);
            SetupCountMocks(feedbackId, 0, 1);

            var result = await _voteService.Vote(feedbackId, "down", userId);

            _voteRepoMock.Verify(r => r.Update(existingVote), Times.Once);
            Assert.Equal("down", result.UserVote);
        }

        // UTCID04: Feedback not found → throws DomainException
        [Fact]
        public async Task Vote_FeedbackNotFound_ThrowsException()
        {
            _feedbackRepoMock.Setup(r => r.GetById(It.IsAny<int>())).ReturnsAsync((BO.Entities.Feedback?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _voteService.Vote(99, "up", 1));

            Assert.Contains("Không tìm thấy đánh giá", ex.Message);
        }

        // UTCID05: User votes on own feedback → throws DomainException
        [Fact]
        public async Task Vote_OwnFeedback_ThrowsException()
        {
            var userId = 5;
            var feedbackId = 5;
            var feedback = new BO.Entities.Feedback { FeedbackId = feedbackId, UserId = userId }; // same user

            _feedbackRepoMock.Setup(r => r.GetById(feedbackId)).ReturnsAsync(feedback);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _voteService.Vote(feedbackId, "up", userId));

            Assert.Contains("chính mình", ex.Message);
        }

        // UTCID06: Invalid vote type string → throws DomainException
        [Fact]
        public async Task Vote_InvalidVoteType_ThrowsException()
        {
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _voteService.Vote(1, "middle", 1));

            Assert.Contains("'up' hoặc 'down'", ex.Message);
        }

        // UTCID07: Returns correct NetScore = UpVotes - DownVotes
        [Fact]
        public async Task Vote_ReturnsCorrectNetScore()
        {
            var feedbackId = 7;
            var userId = 70;
            var feedback = new BO.Entities.Feedback { FeedbackId = feedbackId, UserId = 99 };

            _feedbackRepoMock.Setup(r => r.GetById(feedbackId)).ReturnsAsync(feedback);
            _voteRepoMock.Setup(r => r.GetByFeedbackAndUser(feedbackId, userId)).ReturnsAsync((FeedbackVote?)null);
            _voteRepoMock.Setup(r => r.Create(It.IsAny<FeedbackVote>())).ReturnsAsync(new FeedbackVote());
            SetupCountMocks(feedbackId, 5, 2); // 5 up, 2 down → net = 3

            var result = await _voteService.Vote(feedbackId, "up", userId);

            Assert.Equal(5, result.UpVotes);
            Assert.Equal(2, result.DownVotes);
            Assert.Equal(3, result.NetScore);
        }
    }
}
