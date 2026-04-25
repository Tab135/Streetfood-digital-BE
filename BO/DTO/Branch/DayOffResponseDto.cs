using System;

namespace BO.DTO.Branch
{
    public class DayOffResponseDto
    {
        public int DayOffId { get; set; }
        public int BranchId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
