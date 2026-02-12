using BO.DTO.FeedbackTag;
using BO.Entities;
using Repository.Interfaces;
using Service.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service
{
    public class FeedbackTagService : IFeedbackTagService
    {
        private readonly IFeedbackTagRepository _repo;

        public FeedbackTagService(IFeedbackTagRepository repo)
        {
            _repo = repo;
        }

        public async Task<FeedbackTagDto> CreateFeedbackTag(CreateFeedbackTagDto createDto)
        {
            var entity = new FeedbackTag
            {
                TagName = createDto.TagName,
                Description = createDto.Description
            };

            var created = await _repo.Create(entity);
            return MapToDto(created);
        }

        public async Task<bool> DeleteFeedbackTag(int id)
        {
            var exists = await _repo.Exists(id);
            if (!exists) throw new System.Exception($"Feedback tag with id {id} not found");
            return await _repo.Delete(id);
        }

        public async Task<List<FeedbackTagDto>> GetAllFeedbackTags()
        {
            var list = await _repo.GetAll();
            return list.Select(MapToDto).ToList();
        }

        public async Task<FeedbackTagDto?> GetFeedbackTagById(int id)
        {
            var t = await _repo.GetById(id);
            return t == null ? null : MapToDto(t);
        }

        public async Task<FeedbackTagDto> UpdateFeedbackTag(int id, UpdateFeedbackTagDto updateDto)
        {
            var existing = await _repo.GetById(id);
            if (existing == null)
            {
                throw new System.Exception($"Feedback tag with id {id} not found");
            }

            if (!string.IsNullOrEmpty(updateDto.TagName))
            {
                existing.TagName = updateDto.TagName;
            }

            if (updateDto.Description != null)
            { 
            existing.Description = updateDto.Description;
            }

            var updated = await _repo.Update(existing);
            return MapToDto(updated);
        }

        private FeedbackTagDto MapToDto(FeedbackTag t)
        {
            return new FeedbackTagDto
            {
                TagId = t.TagId,
                TagName = t.TagName,
                Description = t.Description
            };
        }
    }
}