using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL
{
    public class FeedbackTagDAO
    {
        private readonly StreetFoodDbContext _context;
        public FeedbackTagDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        public async Task<List<FeedbackTag>> GetAll()
        {
            return await _context.FeedbackTags.AsNoTracking().ToListAsync();
        }

        public async Task<FeedbackTag?> GetById(int id)
        {
            return await _context.FeedbackTags.FirstOrDefaultAsync(x => x.TagId == id);
        }

        public async Task<FeedbackTag> Create(FeedbackTag feedbackTag)
        {
            feedbackTag.TagId = 0;
            _context.FeedbackTags.Add(feedbackTag);
            await _context.SaveChangesAsync();
            return feedbackTag;
        }

        public async Task<FeedbackTag> Update(FeedbackTag feedbackTag)
        {
            _context.FeedbackTags.Update(feedbackTag);
            await _context.SaveChangesAsync();
            return feedbackTag;
        }

        public async Task<bool> Delete(int id)
        {
            var existing = await _context.FeedbackTags.FirstOrDefaultAsync(x => x.TagId == id);
            if (existing == null) return false;
            _context.FeedbackTags.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Exists(int id)
        {
            return await _context.FeedbackTags.AnyAsync(x => x.TagId == id);
        }
        public async Task<bool> IsInUseAsync(int id)
        {
            return await _context.FeedbackTagAssociations.AnyAsync(x => x.TagId == id);
        }    }
}