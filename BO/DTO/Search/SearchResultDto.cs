using System.Collections.Generic;

namespace BO.DTO.Search
{
    public class SearchResultDto
    {
        public int VendorId { get; set; }
        public string VendorName { get; set; }
        public int ManagerId { get; set; }
        public bool IsActive { get; set; }
        public List<BranchSearchDto> Branches { get; set; } = new List<BranchSearchDto>();
    }

    public class BranchSearchDto
    {
        public int BranchId { get; set; }
        public string Name { get; set; }
        public string AddressDetail { get; set; }
        public string City { get; set; }
        public string? Ward { get; set; }
        public double Lat { get; set; }
        public double Long { get; set; }
        public double AvgRating { get; set; }
        public int TotalReviewCount { get; set; }
        public double FinalScore { get; set; }
        public double DistanceKm { get; set; }
        public bool IsSubscribed { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }

        /// <summary>
        /// DisplayName bucket score for this branch against the search keyword.
        /// 100 = exact DisplayName/vendor/branch match, 80 = fuzzy match,
        /// 50–70 = similar (KFC Rule) match via anchor brand best-sellers, 0 = no name signal.
        /// </summary>
        public double DisplayNameScore { get; set; }

        /// <summary>
        /// Highest Dish Name bucket score across this branch's matching dishes.
        /// 0 when no dish matches the keyword.
        /// </summary>
        public double DishScore { get; set; }

        /// <summary>
        /// Sibling branches of the same vendor that also matched the keyword,
        /// ordered by DistanceKm ascending. Empty when this vendor has a single matching branch.
        /// </summary>
        public List<BranchSearchDto> OtherBranches { get; set; } = new List<BranchSearchDto>();

        public List<DishSearchDto> Dishes { get; set; } = new List<DishSearchDto>();
    }

    public class DishSearchDto
    {
        public int DishId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsSoldOut { get; set; }
        public string CategoryName { get; set; }

        /// <summary>
        /// Dish Name bucket score against the search keyword.
        /// Base buckets: 100 exact / 70–90 in-order / 40–60 out-of-order / 0 no match.
        /// Includes +10 best-seller bonus and +5 high-rating bonus, capped at 120.
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// True when this dish is among its vendor's top completed-order quantity dishes.
        /// </summary>
        public bool IsBestSeller { get; set; }

        /// <summary>
        /// True when the vendor has explicitly flagged this dish as a signature item.
        /// </summary>
        public bool IsSignature { get; set; }
    }
}
