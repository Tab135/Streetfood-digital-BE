using System;

namespace BO.DTO.Branch
{
    public class WorkScheduleResponseDto
    {
        public int WorkScheduleId { get; set; }
        public int BranchId { get; set; }
        public int Weekday { get; set; }
        public string WeekdayName { get; set; } // e.g., "Monday"
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }
    }
}
