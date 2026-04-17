using BO.Common;
using BO.DTO.FeedbackTag;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackTagController : ControllerBase
    {
        private readonly IFeedbackTagService _service;

        public FeedbackTagController(IFeedbackTagService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<FeedbackTagDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<FeedbackTagDto>>> GetAll()
        {
            var list = await _service.GetAllFeedbackTags();
            return Ok(list);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FeedbackTagDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<FeedbackTagDto>> GetById(int id)
        {
            var tag = await _service.GetFeedbackTagById(id);
            if (tag == null) return NotFound();
            return Ok(tag);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<FeedbackTagDto>), StatusCodes.Status201Created)]
        public async Task<ActionResult<FeedbackTagDto>> Create([FromBody] CreateFeedbackTagDto createDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = await _service.CreateFeedbackTag(createDto);
            return CreatedAtAction(nameof(GetById), new { id = created.TagId }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<FeedbackTagDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<FeedbackTagDto>> Update(int id, [FromBody] UpdateFeedbackTagDto updateDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var updated = await _service.UpdateFeedbackTag(id, updateDto);
            return Ok(updated);
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> Delete(int id)
        {
            await _service.DeleteFeedbackTag(id);
            return NoContent();
        }
    }
}