using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [Route("api/searchVendorWithBranch")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string? keyword, [FromQuery] double? userLat = null, [FromQuery] double? userLong = null)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new
                {
                        keyword = new[] { "The keyword field is required." }
                    
                });
            }

            var trimmedKeyword = keyword.Trim();

            if (trimmedKeyword.Length < 2)
            {
                return BadRequest(new
                {

                        keyword = new[] { "Search keyword must be at least 2 characters" }
                    
                });
            }

            var results = await _searchService.SearchAsync(trimmedKeyword, userLat, userLong);

            return Ok(new
            {
                keyword = trimmedKeyword,
                totalResults = results.Count,
                results
            });
        }
    }
}
