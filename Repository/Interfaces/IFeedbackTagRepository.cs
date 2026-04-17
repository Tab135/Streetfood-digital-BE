using BO.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IFeedbackTagRepository
    {
        Task<FeedbackTag> Create(FeedbackTag tag);
        Task<FeedbackTag?> GetById(int tagId);
        Task<List<FeedbackTag>> GetAll();
        Task<FeedbackTag> Update(FeedbackTag tag);
        Task<bool> Delete(int tagId);
        Task<bool> Exists(int tagId);
        Task<bool> IsInUseAsync(int id);
    }
}