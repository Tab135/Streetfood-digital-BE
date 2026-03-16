using BO.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL;

public class VendorReplyDAO
{
    private readonly StreetFoodDbContext _context;

    public VendorReplyDAO(StreetFoodDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<VendorReply?> GetByFeedbackIdAsync(int feedbackId)
    {
        return await _context.VendorReplies
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.FeedbackId == feedbackId);
    }

    public async Task<VendorReply> CreateAsync(VendorReply reply)
    {
        _context.VendorReplies.Add(reply);
        await _context.SaveChangesAsync();
        return reply;
    }

    public async Task UpdateAsync(VendorReply reply)
    {
        reply.UpdatedAt = DateTime.UtcNow;
        _context.VendorReplies.Update(reply);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(VendorReply reply)
    {
        _context.VendorReplies.Remove(reply);
        await _context.SaveChangesAsync();
    }
}
