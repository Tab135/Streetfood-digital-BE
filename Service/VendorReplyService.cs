using BO.DTO.Feedback;
using BO.Entities;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service;

public class VendorReplyService : IVendorReplyService
{
    private readonly IVendorReplyRepository _replyRepository;
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IVendorRepository _vendorRepository;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;

    public VendorReplyService(
        IVendorReplyRepository replyRepository,
        IFeedbackRepository feedbackRepository,
        IBranchRepository branchRepository,
        IVendorRepository vendorRepository,
        INotificationService notificationService,
        IUserRepository userRepository)
    {
        _replyRepository = replyRepository ?? throw new ArgumentNullException(nameof(replyRepository));
        _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
        _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
        _vendorRepository = vendorRepository ?? throw new ArgumentNullException(nameof(vendorRepository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<VendorReplyDto> CreateReply(int feedbackId, CreateVendorReplyDto dto, int userId)
    {
        var feedback = await _feedbackRepository.GetById(feedbackId);
        if (feedback == null)
            throw new Exception("Feedback not found");

        await ValidateVendorAuthorization(feedback.BranchId, userId);

        var existingReply = await _replyRepository.GetByFeedbackId(feedbackId);
        if (existingReply != null)
            throw new Exception("A reply already exists for this feedback");

        var reply = new VendorReply
        {
            FeedbackId = feedbackId,
            UserId = userId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _replyRepository.Create(reply);

        // Get branch name for notification
        var branch = await _branchRepository.GetByIdAsync(feedback.BranchId);

        // Notify feedback author
        await _notificationService.NotifyAsync(
            feedback.UserId,
            NotificationType.VendorReply,
            "Vendor Reply",
            $"Vendor replied to your review on '{branch?.Name}'",
            feedbackId);

        return await MapToDto(created);
    }

    public async Task<VendorReplyDto> UpdateReply(int feedbackId, UpdateVendorReplyDto dto, int userId)
    {
        var feedback = await _feedbackRepository.GetById(feedbackId);
        if (feedback == null)
            throw new Exception("Feedback not found");

        await ValidateVendorAuthorization(feedback.BranchId, userId);

        var reply = await _replyRepository.GetByFeedbackId(feedbackId);
        if (reply == null)
            throw new Exception("Reply not found");

        reply.Content = dto.Content;
        await _replyRepository.Update(reply);

        return await MapToDto(reply);
    }

    public async Task<bool> DeleteReply(int feedbackId, int userId)
    {
        var feedback = await _feedbackRepository.GetById(feedbackId);
        if (feedback == null)
            throw new Exception("Feedback not found");

        await ValidateVendorAuthorization(feedback.BranchId, userId);

        var reply = await _replyRepository.GetByFeedbackId(feedbackId);
        if (reply == null)
            throw new Exception("Reply not found");

        await _replyRepository.Delete(reply);
        return true;
    }

    private async Task ValidateVendorAuthorization(int branchId, int userId)
    {
        var branch = await _branchRepository.GetByIdAsync(branchId);
        if (branch == null)
            throw new Exception("Branch not found");

        // Check if user is branch manager
        if (branch.ManagerId == userId) return;

        // Check if user is vendor owner
        var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId ?? 0);
        if (vendor != null && vendor.UserId == userId) return;

        throw new UnauthorizedAccessException("Only the branch manager or vendor owner can reply");
    }

    private async Task<VendorReplyDto> MapToDto(VendorReply reply)
    {
        var user = await _userRepository.GetUserById(reply.UserId);
        return new VendorReplyDto
        {
            VendorReplyId = reply.VendorReplyId,
            Content = reply.Content,
            RepliedBy = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Vendor",
            CreatedAt = reply.CreatedAt,
            UpdatedAt = reply.UpdatedAt
        };
    }
}
