using System.Collections.Generic;

namespace BO.DTO.Branch
{
    public class SimilarBranchResponseDto
    {
        public int BranchId { get; set; }
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AddressDetail { get; set; } = string.Empty;
        public string? Ward { get; set; }
        public string City { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Long { get; set; }
        public double AvgRating { get; set; }
        public int TotalReviewCount { get; set; }
        public bool IsSubscribed { get; set; }
        public int CommonDishCount { get; set; }
        public double SimilarityScore { get; set; }
        public List<string> SharedDishNames { get; set; } = new();
    }
}