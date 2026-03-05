using System;

namespace BO.DTO.Branch
{
    /// <summary>
    /// Public branch DTO for normal users - hides vendor-specific fields
    /// </summary>
    public class BranchPublicDto
    {
        public int BranchId { get; set; }
        public int VendorId { get; set; }
        public string VendorName { get; set; }
        public int? UserId { get; set; }
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

    }
}
