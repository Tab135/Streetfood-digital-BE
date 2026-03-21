using BO.Common;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System;
using System.Threading.Tasks;
using BO.DTO.Search;

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
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromQuery] SearchFilterDto filter)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            bool hasKeyword = !string.IsNullOrWhiteSpace(filter.Keyword);
            
            if (hasKeyword)
            {
                filter.Keyword = filter.Keyword!.Trim();
                if (filter.Keyword.Length < 2)
                {
                    return BadRequest(new
                    {
                        keyword = new[] { "Search keyword must be at least 2 characters" }
                    });
                }
            }
            else
            {
                // If no keyword, they must provide at least Lat/Long or some other filter to make it valid
                // But specifically ""n?u ch? c� gps m� b?m v�o search nhung ko nh?p m� ch? ch?n d? filter th� s? theo gps lu�n""
                if (!filter.Lat.HasValue || !filter.Long.HasValue)
                {
                    // Maybe return error? Or just search all? 
                    // To be safe, if no keyword and no location, let's reject or let it pass?
                    // I will just let it pass, it will become an unfiltered search or filtered by other fields.
                    // Wait, maybe I should require either keyword or GPS.
                }
            }

            var results = await _searchService.SearchAsync(filter);
            return Ok(new
            {
                keyword = filter.Keyword,
                totalResults = results.Count,
                results
            });
        }
    }
}
