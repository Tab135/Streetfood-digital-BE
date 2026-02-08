using System;

namespace BO.DTO.Branch
{
    public class DayOffResponseDto
    {
        public int DayOffId { get; set; }
        public int BranchId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
    }
}
