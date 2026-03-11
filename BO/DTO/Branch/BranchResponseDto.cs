using System;

namespace BO.DTO.Branch
{
    public class BranchResponseDto
    {
        public int BranchId { get; set; }
        public int VendorId { get; set; }
        public int? ManagerId { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string AddressDetail { get; set; }
        public string Ward { get; set; }
        public string City { get; set; }
        public double Lat { get; set; }
        public double Long { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsVerified { get; set; }
        public double AvgRating { get; set; }
        public bool IsActive { get; set; }
        public bool IsSubscribed { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
        public int? DaysRemaining { get; set; }
        
        // License info
        public System.Collections.Generic.List<string> LicenseUrls { get; set; }
        public string LicenseStatus { get; set; } // Pending, Accept, Reject
        public string LicenseRejectReason { get; set; }
    }
}
