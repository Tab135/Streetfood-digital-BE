using System;
using System.Collections.Generic;

namespace BO.DTO.Dashboard
{
    public class AdminUserSignupChartDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalSignupCount { get; set; }
        public List<AdminUserSignupPointDto> DailySignups { get; set; } = new List<AdminUserSignupPointDto>();
    }

    public class AdminUserSignupPointDto
    {
        public DateTime Date { get; set; }
        public int SignupCount { get; set; }
    }

    public class AdminMoneyChartDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalBranchRegistrationAmount { get; set; }
        public decimal TotalSystemCampaignAmount { get; set; }
        public List<AdminMoneyPointDto> DailyAmounts { get; set; } = new List<AdminMoneyPointDto>();
    }

    public class AdminMoneyPointDto
    {
        public DateTime Date { get; set; }
        public decimal BranchRegistrationAmount { get; set; }
        public decimal SystemCampaignAmount { get; set; }
    }

    public class AdminCompensationChartDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalCompensationAmount { get; set; }
        public List<AdminCompensationPointDto> DailyCompensations { get; set; } = new List<AdminCompensationPointDto>();
    }

    public class AdminCompensationPointDto
    {
        public DateTime Date { get; set; }
        public decimal CompensationAmount { get; set; }
    }

    public class AdminUserToVendorConversionChartDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalConversionCount { get; set; }
        public List<AdminUserToVendorConversionPointDto> DailyConversions { get; set; } = new List<AdminUserToVendorConversionPointDto>();
    }

    public class AdminUserToVendorConversionPointDto
    {
        public DateTime Date { get; set; }
        public int ConversionCount { get; set; }
    }
}
