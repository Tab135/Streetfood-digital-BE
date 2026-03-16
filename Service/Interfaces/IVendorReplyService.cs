using BO.DTO.Feedback;

namespace Service.Interfaces;

public interface IVendorReplyService
{
    Task<VendorReplyDto> CreateReply(int feedbackId, CreateVendorReplyDto dto, int userId);
    Task<VendorReplyDto> UpdateReply(int feedbackId, UpdateVendorReplyDto dto, int userId);
    Task<bool> DeleteReply(int feedbackId, int userId);
}
