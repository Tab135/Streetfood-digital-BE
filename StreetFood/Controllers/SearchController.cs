using BO.Common;
using BO.DTO.Search;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
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
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search(
            [FromQuery] SearchFilterDto filter,
            [FromQuery(Name = "rankingV2")] int? rankingV2Query)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            bool hasKeyword = !string.IsNullOrWhiteSpace(filter.Keyword);

            if (hasKeyword)
            {
                filter.Keyword = filter.Keyword!.Trim();
                if (filter.Keyword.Length < 1)
                {
                    return BadRequest(new
                    {
                        keyword = new[] { "Search keyword must be at least 1 character" }
                    });
                }
            }

            var results = await _searchService.SearchAsync(filter);

            if (!UseRankingV2(rankingV2Query))
            {
                results = FlattenToLegacyShape(results);
            }

            return Ok(new
            {
                keyword = filter.Keyword,
                totalResults = results.Count,
                results
            });
        }

        // Default to the new ranking. Clients can opt out with `?rankingV2=0` or `X-Search-Version: 1`
        // during rollout to get the legacy flat shape (no OtherBranches grouping).
        private bool UseRankingV2(int? rankingV2Query)
        {
            if (rankingV2Query.HasValue) return rankingV2Query.Value != 0;

            if (Request.Headers.TryGetValue("X-Search-Version", out var header))
            {
                var value = header.ToString();
                if (int.TryParse(value, out var parsed)) return parsed >= 2;
            }

            return true;
        }

        // Legacy shape: one top-level entry per vendor, with every matching branch (primary + siblings)
        // flattened into the single Branches list. No OtherBranches, no DisplayNameScore/DishScore on branches.
        private static List<SearchResultDto> FlattenToLegacyShape(List<SearchResultDto> results)
        {
            return results.Select(v =>
            {
                var primary = v.Branches.FirstOrDefault();
                var allBranches = new List<BranchSearchDto>();
                if (primary != null)
                {
                    var hoisted = primary.OtherBranches ?? new List<BranchSearchDto>();
                    allBranches.Add(primary);
                    allBranches.AddRange(hoisted);

                    // Clear the nested grouping so legacy clients don't see it.
                    primary.OtherBranches = new List<BranchSearchDto>();
                }

                return new SearchResultDto
                {
                    VendorId = v.VendorId,
                    VendorName = v.VendorName,
                    ManagerId = v.ManagerId,
                    IsActive = v.IsActive,
                    Branches = allBranches
                };
            }).ToList();
        }
    }
}
