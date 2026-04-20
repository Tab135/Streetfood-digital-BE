using System;
using System.Threading.Tasks;
using BO.DTO.Feedback;
using BO.Entities;
using BO.Exceptions;
using Moq;
using Repository.Interfaces;
using Service;
using Service.Interfaces;
using Xunit;

namespace StreetFood.Tests.FeedbackTests
{
    public class VendorReplyServiceTests
    {
        private readonly Mock<IVendorReplyRepository> _replyRepoMock;
        private readonly Mock<IFeedbackRepository> _feedbackRepoMock;
        private readonly Mock<IBranchRepository> _branchRepoMock;
        private readonly Mock<IVendorRepository> _vendorRepoMock;
        private readonly Mock<INotificationService> _notifServiceMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly VendorReplyService _replyService;

        public VendorReplyServiceTests()
        {
            _replyRepoMock = new Mock<IVendorReplyRepository>();
            _feedbackRepoMock = new Mock<IFeedbackRepository>();
            _branchRepoMock = new Mock<IBranchRepository>();
            _vendorRepoMock = new Mock<IVendorRepository>();
            _notifServiceMock = new Mock<INotificationService>();
            _userRepoMock = new Mock<IUserRepository>();

            _replyService = new VendorReplyService(
                _replyRepoMock.Object,
                _feedbackRepoMock.Object,
                _branchRepoMock.Object,
                _vendorRepoMock.Object,
                _notifServiceMock.Object,
                _userRepoMock.Object
            );
        }

        // --- SECTION: CREATE REPLY (SV_VREPLY_01) ---

        [Fact]
        public async Task CreateReply_BranchManager_Success()
        {
            var userId = 1; var feedbackId = 50; var branchId = 5;
            var feedback = new Feedback { FeedbackId = feedbackId, BranchId = branchId, UserId = 10 };
            var branch = new BO.Entities.Branch { BranchId = branchId, ManagerId = userId, VendorId = 2 };
            var vendor = new BO.Entities.Vendor { VendorId = 2, Name = "Good Food", UserId = 99 };

            _feedbackRepoMock.Setup(r => r.GetById(feedbackId)).ReturnsAsync(feedback);
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(vendor);
            _replyRepoMock.Setup(r => r.GetByFeedbackId(feedbackId)).ReturnsAsync((VendorReply?)null);
            _replyRepoMock.Setup(r => r.Create(It.IsAny<VendorReply>())).ReturnsAsync(new VendorReply { FeedbackId = feedbackId });
            _userRepoMock.Setup(r => r.GetUserById(userId)).ReturnsAsync(new User { FirstName = "Manager" });

            var result = await _replyService.CreateReply(feedbackId, new CreateVendorReplyDto { Content = "Thanks" }, userId);

            Assert.NotNull(result);
            _replyRepoMock.Verify(r => r.Create(It.IsAny<VendorReply>()), Times.Once);
            _notifServiceMock.Verify(s => s.NotifyAsync(10, NotificationType.VendorReply, It.IsAny<string>(), It.IsAny<string>(), feedbackId, It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task CreateReply_VendorOwner_Success()
        {
            var userId = 2; var feedbackId = 50; var branchId = 5;
            var feedback = new Feedback { FeedbackId = feedbackId, BranchId = branchId };
            var branch = new BO.Entities.Branch { BranchId = branchId, ManagerId = 99, VendorId = 2 }; // Manager is 99
            var vendor = new BO.Entities.Vendor { VendorId = 2, UserId = userId }; // Owner is userId

            _feedbackRepoMock.Setup(r => r.GetById(feedbackId)).ReturnsAsync(feedback);
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(vendor);
            _replyRepoMock.Setup(r => r.GetByFeedbackId(feedbackId)).ReturnsAsync((VendorReply?)null);
            _replyRepoMock.Setup(r => r.Create(It.IsAny<VendorReply>())).ReturnsAsync(new VendorReply { FeedbackId = feedbackId });

            var result = await _replyService.CreateReply(feedbackId, new CreateVendorReplyDto { Content = "Thanks" }, userId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreateReply_FeedbackNotFound_ThrowsException()
        {
            _feedbackRepoMock.Setup(r => r.GetById(It.IsAny<int>())).ReturnsAsync((Feedback?)null);
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _replyService.CreateReply(99, new CreateVendorReplyDto(), 1));
            Assert.Equal("Không tìm thấy đánh giá", ex.Message);
        }

        [Fact]
        public async Task CreateReply_AlreadyReplied_ThrowsException()
        {
            var feedback = new Feedback { FeedbackId = 50, BranchId = 5 };
            var branch = new BO.Entities.Branch { BranchId = 5, ManagerId = 1 };
            
            _feedbackRepoMock.Setup(r => r.GetById(50)).ReturnsAsync(feedback);
            _branchRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(branch);
            _replyRepoMock.Setup(r => r.GetByFeedbackId(50)).ReturnsAsync(new VendorReply());

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _replyService.CreateReply(50, new CreateVendorReplyDto(), 1));
            Assert.Equal("Đã có phản hồi cho đánh giá này", ex.Message);
        }

        [Fact]
        public async Task CreateReply_Unauthorized_ThrowsException()
        {
            var feedback = new Feedback { FeedbackId = 50, BranchId = 5 };
            var branch = new BO.Entities.Branch { BranchId = 5, ManagerId = 2, VendorId = 2 }; // Mgr is 2
            var vendor = new BO.Entities.Vendor { VendorId = 2, UserId = 3 }; // Owner is 3

            _feedbackRepoMock.Setup(r => r.GetById(50)).ReturnsAsync(feedback);
            _branchRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(vendor);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _replyService.CreateReply(50, new CreateVendorReplyDto(), 9));
            Assert.Contains("mới có thể trả lời", ex.Message);
        }

        [Fact]
        public async Task CreateReply_BranchNotFound_ThrowsException()
        {
            var feedback = new Feedback { FeedbackId = 50, BranchId = 5 };
            _feedbackRepoMock.Setup(r => r.GetById(50)).ReturnsAsync(feedback);
            _branchRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync((BO.Entities.Branch?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _replyService.CreateReply(50, new CreateVendorReplyDto(), 1));
            Assert.Equal("Không tìm thấy chi nhánh", ex.Message);
        }

        [Fact]
        public async Task CreateReply_VendorNotFound_Unauthorized_ThrowsException()
        {
            var feedback = new Feedback { FeedbackId = 50, BranchId = 5 };
            var branch = new BO.Entities.Branch { BranchId = 5, ManagerId = 99, VendorId = 0 }; // Not manager
            
            _feedbackRepoMock.Setup(r => r.GetById(50)).ReturnsAsync(feedback);
            _branchRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(0)).ReturnsAsync((BO.Entities.Vendor?)null);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _replyService.CreateReply(50, new CreateVendorReplyDto(), 1));
            Assert.Contains("mới có thể trả lời", ex.Message);
        }

        // --- SECTION: UPDATE REPLY (SV_VREPLY_02) ---

        // UTCID01: Success - Branch Manager update
        [Fact]
        public async Task UpdateReply_Success_Manager()
        {
            var userId = 1; var feedbackId = 50;
            var feedback = new Feedback { FeedbackId = feedbackId, BranchId = 5 };
            var branch = new BO.Entities.Branch { BranchId = 5, ManagerId = userId };
            var reply = new VendorReply { VendorReplyId = 1, FeedbackId = feedbackId, Content = "Old" };

            _feedbackRepoMock.Setup(r => r.GetById(feedbackId)).ReturnsAsync(feedback);
            _branchRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(branch);
            _replyRepoMock.Setup(r => r.GetByFeedbackId(feedbackId)).ReturnsAsync(reply);

            var result = await _replyService.UpdateReply(feedbackId, new UpdateVendorReplyDto { Content = "New" }, userId);

            Assert.Equal("New", result.Content);
        }

        // UTCID02: Feedback Not Found
        [Fact]
        public async Task UpdateReply_FeedbackNotFound_ThrowsException()
        {
            _feedbackRepoMock.Setup(r => r.GetById(It.IsAny<int>())).ReturnsAsync((Feedback?)null);
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _replyService.UpdateReply(50, new UpdateVendorReplyDto(), 1));
            Assert.Equal("Không tìm thấy đánh giá", ex.Message);
        }

        // UTCID03: Reply Not Found
        [Fact]
        public async Task UpdateReply_ReplyNotFound_ThrowsException()
        {
            var feedback = new Feedback { FeedbackId = 50, BranchId = 5 };
            var branch = new BO.Entities.Branch { BranchId = 5, ManagerId = 1 };
            _feedbackRepoMock.Setup(r => r.GetById(50)).ReturnsAsync(feedback);
            _branchRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(branch);
            _replyRepoMock.Setup(r => r.GetByFeedbackId(50)).ReturnsAsync((VendorReply?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _replyService.UpdateReply(50, new UpdateVendorReplyDto(), 1));
            Assert.Equal("Không tìm thấy phản hồi", ex.Message);
        }
        
        // UTCID04: Unauthorized (Random User)
        [Fact]
        public async Task UpdateReply_Unauthorized_ThrowsException()
        {
             var feedback = new Feedback { FeedbackId = 50, BranchId = 5 };
            var branch = new BO.Entities.Branch { BranchId = 5, ManagerId = 2, VendorId = 2 };
            var vendor = new BO.Entities.Vendor { VendorId = 2, UserId = 3 }; // Owner is 3
            
            _feedbackRepoMock.Setup(r => r.GetById(50)).ReturnsAsync(feedback);
            _branchRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(vendor);
            
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _replyService.UpdateReply(50, new UpdateVendorReplyDto(), 9));
            Assert.Contains("mới có thể trả lời", ex.Message);
        }

        // UTCID05: Unauthorized (Owner of a DIFFERENT vendor)
        [Fact]
        public async Task UpdateReply_MismatchVendorOwner_ThrowsException()
        {
            var userId = 1; var feedbackId = 50;
            var feedback = new Feedback { FeedbackId = feedbackId, BranchId = 5 };
            var branch = new BO.Entities.Branch { BranchId = 5, ManagerId = 99, VendorId = 2 };
            var vendor = new BO.Entities.Vendor { VendorId = 2, UserId = 88 }; // Not userId

            _feedbackRepoMock.Setup(r => r.GetById(feedbackId)).ReturnsAsync(feedback);
            _branchRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(vendor);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _replyService.UpdateReply(feedbackId, new UpdateVendorReplyDto(), userId));
            Assert.Contains("mới có thể trả lời", ex.Message);
        }

        // UTCID06: Unauthorized (Manager of a DIFFERENT branch)
        [Fact]
        public async Task UpdateReply_MismatchBranchManager_ThrowsException()
        {
            var userId = 1; var feedbackId = 50;
            var feedback = new Feedback { FeedbackId = feedbackId, BranchId = 5 };
            var branch = new BO.Entities.Branch { BranchId = 5, ManagerId = 99, VendorId = 2 };
            var vendor = new BO.Entities.Vendor { VendorId = 2, UserId = 88 };

            _feedbackRepoMock.Setup(r => r.GetById(feedbackId)).ReturnsAsync(feedback);
            _branchRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(vendor);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _replyService.UpdateReply(50, new UpdateVendorReplyDto(), userId));
            Assert.Contains("mới có thể trả lời", ex.Message);
        }

        // UTCID07: Success - Vendor Owner update
        [Fact]
        public async Task UpdateReply_Success_Owner()
        {
            var userId = 1; var feedbackId = 50;
            var feedback = new Feedback { FeedbackId = feedbackId, BranchId = 5 };
            var branch = new BO.Entities.Branch { BranchId = 5, ManagerId = 99, VendorId = 2 };
            var vendor = new BO.Entities.Vendor { VendorId = 2, UserId = userId }; // Is owner
            var reply = new VendorReply { FeedbackId = feedbackId };

            _feedbackRepoMock.Setup(r => r.GetById(feedbackId)).ReturnsAsync(feedback);
            _branchRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(vendor);
            _replyRepoMock.Setup(r => r.GetByFeedbackId(feedbackId)).ReturnsAsync(reply);
            _replyRepoMock.Setup(r => r.Update(reply)).Returns(Task.CompletedTask);

            var result = await _replyService.UpdateReply(feedbackId, new UpdateVendorReplyDto { Content = "Ok" }, userId);
            Assert.Equal("Ok", result.Content);
        }
    }
}
