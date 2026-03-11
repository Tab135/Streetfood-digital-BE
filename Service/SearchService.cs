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
            
            // Search for vendors with their branches and dishes
            var branches = await _branchRepository.SearchVendorsWithBranchesAndDishesAsync(keyword);

            // Filter branches that match the search keyword (by branch name or dish name)
            var matchingBranches = branches
                .Where(branch => 
                    TextNormalizer.NormalizeForSearch(branch.Name).Contains(normalizedKeyword) ||
                    branch.Dishes.Any(d => TextNormalizer.NormalizeForSearch(d.Name).Contains(normalizedKeyword)))
                .ToList();

            // Group branches by Vendor and return vendor-centric results
            var vendorResults = matchingBranches
                .GroupBy(branch => new 
                { 
                    branch.VendorId, 
                    VendorName = branch.Vendor?.Name,
                    branch.ManagerId,
                    VendorIsActive = branch.Vendor?.IsActive
                })
                .Select(vendorGroup => new SearchResultDto
                {
                    VendorId = vendorGroup.Key.VendorId,
                    VendorName = vendorGroup.Key.VendorName ?? string.Empty,
                    ManagerId = vendorGroup.Key.ManagerId ?? 0,
                    IsActive = vendorGroup.Key.VendorIsActive ?? false,
                    Branches = vendorGroup.Select(branch => new BranchSearchDto
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
                    }).ToList()
                })
                .ToList();

            return vendorResults;
        }
    }
}
