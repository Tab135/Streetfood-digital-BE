using BO.Common;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    /// <summary>
    /// Global Search Controller - Search across branches and dishes
    /// </summary>
    [Route("api/search")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        }

        /// <summary>
        /// Search for branches and dishes by keyword
        /// </summary>
        /// <param name="keyword">Search keyword (e.g., "banh cuon")</param>
        /// <returns>List of branches with matching dishes</returns>
        /// <remarks>
        /// Search logic:
        /// - Searches in branch names (case-insensitive)
        /// - Searches in dish names (case-insensitive)
        /// - Returns only active and verified branches
        /// - Returns only active dishes
        /// - Results ordered by branch rating (highest first)
        /// 
        /// Example: GET /api/search?keyword=banh cuon
        /// </remarks>
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

                // Trim the keyword to avoid unnecessary whitespace
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
