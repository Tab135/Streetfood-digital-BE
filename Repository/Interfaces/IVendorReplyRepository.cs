using BO.Entities;

namespace Repository.Interfaces;

public interface IVendorReplyRepository
{
    Task<VendorReply?> GetByFeedbackId(int feedbackId);
    Task<VendorReply> Create(VendorReply reply);
    Task Update(VendorReply reply);
    Task Delete(VendorReply reply);
}
