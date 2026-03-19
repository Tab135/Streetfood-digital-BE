using System.Collections.Generic;

namespace BO.DTO.Feedback
{
    public class VelocityCheckDto
    {
        public int RemainingTotalToday { get; set; }
        public int DailyLimit { get; set; }
        public List<int> ReviewedBranchIds { get; set; } = new List<int>();
    }
}
