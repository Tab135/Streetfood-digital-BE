using System.Collections.Generic;

namespace BO.DTO.Feedback
{
    public class VelocityCheckDto
    {
        public int RemainingTotalToday { get; set; }
        public int DailyLimit { get; set; }
        public List<int> ReviewedBranchIds { get; set; } = new List<int>();
        public int? BranchId { get; set; }
        public double? DistanceMeters { get; set; }
        public double? MaxDistanceMeters { get; set; }
        public bool? IsWithinDistance { get; set; }
        public bool? CanReviewWithoutOrder { get; set; }
    }
}
