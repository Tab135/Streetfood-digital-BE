using System;
using System.Collections.Generic;
using BO.Entities;

namespace BO.DTO.Branch
{
    public class PendingRegistrationDto
    {
        public int BranchRegisterRequestId { get; set; }
        public int BranchId { get; set; }
        public string? LicenseUrl { get; set; }
        public RegisterVendorStatusEnum Status { get; set; }
        public string? RejectReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public PendingBranchInfo Branch { get; set; }

        public class PendingBranchInfo
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
            public List<BranchImageResponseDto> BranchImages { get; set; }
        }
    }
}
