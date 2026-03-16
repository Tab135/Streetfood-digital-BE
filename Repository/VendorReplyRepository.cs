using BO.Entities;
using DAL;
using Repository.Interfaces;

namespace Repository;

public class VendorReplyRepository : IVendorReplyRepository
{
    private readonly VendorReplyDAO _vendorReplyDAO;

    public VendorReplyRepository(VendorReplyDAO vendorReplyDAO)
    {
        _vendorReplyDAO = vendorReplyDAO ?? throw new ArgumentNullException(nameof(vendorReplyDAO));
    }

    public async Task<VendorReply?> GetByFeedbackId(int feedbackId) =>
        await _vendorReplyDAO.GetByFeedbackIdAsync(feedbackId);

    public async Task<VendorReply> Create(VendorReply reply) =>
        await _vendorReplyDAO.CreateAsync(reply);

    public async Task Update(VendorReply reply) =>
        await _vendorReplyDAO.UpdateAsync(reply);

    public async Task Delete(VendorReply reply) =>
        await _vendorReplyDAO.DeleteAsync(reply);
}
