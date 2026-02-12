using BO.Entities;
using DAL;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class FeedbackTagRepository : IFeedbackTagRepository
    {
        private readonly FeedbackTagDAO _dao;

        public FeedbackTagRepository(FeedbackTagDAO dao)
        {
            _dao = dao ?? throw new ArgumentNullException(nameof(dao));
        }

        public async Task<FeedbackTag> Create(FeedbackTag tag)
        {
            return await _dao.Create(tag);
        }

        public async Task<FeedbackTag?> GetById(int tagId)
        {
            return await _dao.GetById(tagId);
        }

        public async Task<List<FeedbackTag>> GetAll()
        {
            return await _dao.GetAll();
        }

        public async Task<FeedbackTag> Update(FeedbackTag tag)
        {
            return await _dao.Update(tag);
        }

        public async Task<bool> Delete(int tagId)
        {
            return await _dao.Delete(tagId);
        }

        public async Task<bool> Exists(int tagId)
        {
            return await _dao.Exists(tagId);
        }
    }
}