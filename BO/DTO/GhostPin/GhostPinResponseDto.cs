using System;

namespace BO.DTO.GhostPin
{
    public class GhostPinResponseDto
    {
        public int GhostPinId { get; set; }
        public int CreatorId { get; set; }
        public string Name { get; set; }
        public string AddressDetail { get; set; }
        public string? Ward { get; set; }
        public string City { get; set; }
        public double Lat { get; set; }
        public double Long { get; set; }
        
        public bool IsVerified { get; set; }
        
        public double AvgRating { get; set; }
        public int TotalReviewCount { get; set; }
        public int TotalRatingSum { get; set; }
        public int BatchReviewCount { get; set; }
        public int BatchRatingSum { get; set; }
        public int TierId { get; set; }
        public DateTime? LastTierResetAt { get; set; }

        public string? RejectReason { get; set; }
        public int? LinkedBranchId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
