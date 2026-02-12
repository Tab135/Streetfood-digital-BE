using BO.Common;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [Route("api/search")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string? keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return BadRequest(new ApiResponse<object>(400, "The keyword field is required.", new
                    {
                        keyword = new[] { "The keyword field is required." }
                    }));
                }

                var trimmedKeyword = keyword.Trim();

                if (trimmedKeyword.Length < 2)
                {
                    return BadRequest(new ApiResponse<object>(400, "Search keyword must be at least 2 characters", new
                    {
                        keyword = new[] { "Search keyword must be at least 2 characters" }
                    }));
                }

                var results = await _searchService.SearchAsync(trimmedKeyword);

                return Ok(new ApiResponse<object>(200, "Search completed successfully", new
                {
                    keyword = trimmedKeyword,
                    totalResults = results.Count,
                    results
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>(500, $"An error occurred during search: {ex.Message}", null));
            }
        }
    }
}
