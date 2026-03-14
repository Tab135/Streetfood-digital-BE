using System;
using System.Collections.Generic;
using BO.DTO.Branch;

namespace BO.DTO.Vendor
{
    public class VendorOwnerDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string AvatarUrl { get; set; }
    }

    public class VendorResponseDto
    {
        public int VendorId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public VendorOwnerDto VendorOwner { get; set; }
        public List<BranchResponseDto> Branches { get; set; }
    }
}
