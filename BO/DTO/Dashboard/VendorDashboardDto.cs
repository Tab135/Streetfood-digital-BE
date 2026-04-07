using System;
using System.Collections.Generic;

namespace BO.DTO.Dashboard
{
    public class RevenueDashboardDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public List<DailyRevenueDto> DailyRevenues { get; set; } = new List<DailyRevenueDto>();
    }

    public class DailyRevenueDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class VoucherDashboardDto
    {
        public List<VoucherUsageDto> VoucherUsages { get; set; } = new List<VoucherUsageDto>();
    }

    public class DishDashboardDto
    {
        public List<TopDishDto> TopDishes { get; set; } = new List<TopDishDto>();
    }

    public class VoucherUsageDto
    {
        public string VoucherType { get; set; } = string.Empty;
        public string VoucherName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
    }

    public class TopDishDto
    {
        public int DishId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public int TotalQuantityOrdered { get; set; }
    }
}
