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

        public async Task<List<SearchResultDto>> SearchAsync(string keyword, double? userLat = null, double? userLong = null)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new List<SearchResultDto>();
            }

            var normalizedKeyword = TextNormalizer.NormalizeForSearch(keyword);
            
            // Search for vendors with their branches and dishes
            var branches = await _branchRepository.SearchVendorsWithBranchesAndDishesAsync(keyword);

// Filter branches that match the search keyword (by vendor name or dish name)
            var matchingBranches = branches
                .Where(branch =>
                    (branch.Vendor != null && TextNormalizer.NormalizeForSearch(branch.Vendor.Name).Contains(normalizedKeyword)) ||
                    branch.BranchDishes.Any(bd => TextNormalizer.NormalizeForSearch(bd.Dish.Name).Contains(normalizedKeyword)))
                .ToList();

            // Calculate final score and sort branches
            var branchesWithScores = matchingBranches.Select(branch => 
            {
                double distance = 0;
                if (userLat.HasValue && userLong.HasValue)
                {
                    distance = HaversineDistance(userLat.Value, userLong.Value, branch.Lat, branch.Long);
                }

                double wDist = 0.6;
                double wRate = 0.4;
                double tierWeight = branch.Tier != null ? branch.Tier.Weight : 1.0;
                double subMultiplier = branch.IsSubscribed ? 1.2 : 0.7;

                double distanceScore = distance == 0 && (!userLat.HasValue || !userLong.HasValue) 
                    ? 0 // If no user location, distance score is 0
                    : (1 / (distance + 1)) * wDist;
                    
                double ratingScore = (branch.AvgRating / 5) * wRate;

                double finalScore = (distanceScore + ratingScore) * tierWeight * subMultiplier;

                return new { Branch = branch, FinalScore = finalScore };
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
                        FinalScore = x.FinalScore,
                        IsVerified = x.Branch.IsVerified,
                        IsActive = x.Branch.IsActive,
                        Dishes = x.Branch.BranchDishes
                            .Where(bd => bd.Dish != null && bd.Dish.IsActive &&
                                TextNormalizer.NormalizeForSearch(bd.Dish.Name).Contains(normalizedKeyword))
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

        private static double HaversineDistance(double lat1, double long1, double lat2, double long2)
        {
            const double EarthRadiusKm = 6371.0;

            double dLat = DegreesToRadians(lat2 - lat1);
            double dLong = DegreesToRadians(long2 - long1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                       Math.Sin(dLong / 2) * Math.Sin(dLong / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
