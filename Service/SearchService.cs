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

        public async Task<List<SearchResultDto>> SearchAsync(SearchFilterDto filter)
        {
            bool hasKeyword = !string.IsNullOrWhiteSpace(filter.Keyword);
            string normalizedKeyword = hasKeyword ? TextNormalizer.NormalizeForSearch(filter.Keyword) : string.Empty;

            double? userLat = filter.Lat;
            double? userLong = filter.Long;

            // Fetch filtered branches dynamically via DAO
            var filteredItems = await _branchRepository.GetActiveBranchesFilteredAsync(
                userLat,
                userLong,
                filter.Distance,
                filter.DietaryIds,
                filter.TasteIds,
                filter.MinPrice,
                filter.MaxPrice,
                filter.CategoryIds
            );

            // Filter by keyword client-side if a keyword is provided
            var matchingItems = filteredItems;
            
            if (hasKeyword)
            {
                matchingItems = matchingItems
                    .Where(item =>
                        (item.branch.Vendor != null && !string.IsNullOrWhiteSpace(item.branch.Vendor.Name) && TextNormalizer.NormalizeForSearch(item.branch.Vendor.Name).Contains(normalizedKeyword)) ||
                        (!string.IsNullOrWhiteSpace(item.branch.Name) && TextNormalizer.NormalizeForSearch(item.branch.Name).Contains(normalizedKeyword)) ||
                        item.branch.BranchDishes.Any(bd => bd.Dish != null && !string.IsNullOrWhiteSpace(bd.Dish.Name) && TextNormalizer.NormalizeForSearch(bd.Dish.Name).Contains(normalizedKeyword)))
                    .ToList();
            }

            // Calculate final score and sort branches
            var branchesWithScores = matchingItems.Select(item =>
            {
                var branch = item.branch;
                double distanceKm = item.distanceKm;

                double wDist = 0.6;
                double wRate = 0.4;
                double tierWeight = branch.Tier != null ? branch.Tier.Weight : 1.0;
                double subMultiplier = branch.IsSubscribed ? 1.2 : 0.7;

                double distanceScore = (distanceKm == 0 && (!userLat.HasValue || !userLong.HasValue))
                    ? 0 // If no user location, distance score is 0
                    : (1 / (distanceKm + 1)) * wDist;

                double ratingScore = (branch.AvgRating / 5) * wRate;

                double finalScore = (distanceScore + ratingScore) * tierWeight * subMultiplier;

                return new { Branch = branch, DistanceKm = distanceKm, FinalScore = finalScore };
            })
            .OrderByDescending(x => x.FinalScore)
            .ToList();

            // Group branches by Vendor and return vendor-centric results
            var vendorResults = branchesWithScores
                .GroupBy(x => new
                {
                    x.Branch.VendorId,
                    VendorName = x.Branch.Vendor?.Name,
                    x.Branch.ManagerId,
                    VendorIsActive = x.Branch.Vendor?.IsActive
                })
                .Select(vendorGroup => new SearchResultDto
                {
                    VendorId = vendorGroup.Key.VendorId ?? 0,
                    VendorName = vendorGroup.Key.VendorName ?? string.Empty,
                    ManagerId = vendorGroup.Key.ManagerId ?? 0,
                    IsActive = vendorGroup.Key.VendorIsActive ?? false,
                    Branches = vendorGroup.Select(x => new BranchSearchDto
                    {
                        BranchId = x.Branch.BranchId,
                        Name = x.Branch.Name,
                        AddressDetail = x.Branch.AddressDetail,
                        City = x.Branch.City,
                        Ward = x.Branch.Ward,
                        Lat = x.Branch.Lat,
                        Long = x.Branch.Long,
                        AvgRating = x.Branch.AvgRating,
                        TotalReviewCount = x.Branch.TotalReviewCount,
                        FinalScore = x.FinalScore,
                        DistanceKm = x.DistanceKm,
                        IsSubscribed = x.Branch.IsSubscribed,
                        IsVerified = x.Branch.IsVerified,
                        IsActive = x.Branch.IsActive,
                        Dishes = x.Branch.BranchDishes
                            .Where(bd => bd.Dish != null && bd.Dish.IsActive && 
                                (!hasKeyword || TextNormalizer.NormalizeForSearch(bd.Dish.Name).Contains(normalizedKeyword)))
                            .Select(bd => new DishSearchDto
                            {
                                DishId = bd.Dish.DishId,
                                Name = bd.Dish.Name,
                                Price = bd.Dish.Price,
                                Description = bd.Dish.Description,
                                ImageUrl = bd.Dish.ImageUrl,
                                IsSoldOut = bd.IsSoldOut,
                                CategoryName = bd.Dish.Category?.Name ?? string.Empty
                            })
                            .ToList()
                    }).ToList()
                })
                .OrderByDescending(v => v.Branches.Max(b => b.FinalScore))
                .ToList();

            return vendorResults;
        }
    }
}
