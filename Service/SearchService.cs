using BO.DTO.Search;
using Repository.Interfaces;
using Service.Interfaces;
using Service.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service
{
    public class SearchService : ISearchService
    {
        private readonly IBranchRepository _branchRepository;

        public SearchService(IBranchRepository branchRepository)
        {
            _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
        }

        public async Task<List<SearchResultDto>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new List<SearchResultDto>();
            }

            var normalizedKeyword = TextNormalizer.NormalizeForSearch(keyword);
            var branches = await _branchRepository.SearchBranchesWithDishesAsync(keyword);

            var results = branches
                .Where(branch => 
                    TextNormalizer.NormalizeForSearch(branch.Name).Contains(normalizedKeyword) ||
                    branch.Dishes.Any(d => TextNormalizer.NormalizeForSearch(d.Name).Contains(normalizedKeyword)))
                .Select(branch => new SearchResultDto
                {
                    BranchId = branch.BranchId,
                    Name = branch.Name,
                    AddressDetail = branch.AddressDetail,
                    City = branch.City,
                    Ward = branch.Ward,
                    Lat = branch.Lat,
                    Long = branch.Long,
                    AvgRating = branch.AvgRating,
                    IsVerified = branch.IsVerified,
                    IsActive = branch.IsActive,
                    Dishes = branch.Dishes
                        .Where(d => d.IsActive && 
                            TextNormalizer.NormalizeForSearch(d.Name).Contains(normalizedKeyword))
                        .Select(dish => new DishSearchDto
                        {
                            DishId = dish.DishId,
                            Name = dish.Name,
                            Price = dish.Price,
                            Description = dish.Description,
                            ImageUrl = dish.ImageUrl,
                            IsSoldOut = dish.IsSoldOut,
                            CategoryName = dish.Category?.Name ?? string.Empty
                        })
                        .ToList()
                })
                .ToList();

            return results;
        }
    }
}
