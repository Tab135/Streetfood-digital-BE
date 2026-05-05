using System;
using System.Collections.Generic;

namespace BO.DTO.Dashboard
{
    public class BarChartItemDto
    {
        public string Label { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal Value { get; set; }
    }

    public class RevenueBarChartDto
    {
        public List<BarChartItemDto> Items { get; set; } = new List<BarChartItemDto>();
    }
}
