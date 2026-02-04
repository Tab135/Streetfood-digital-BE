using System;
using System.Collections.Generic;
using BO.DTO.Branch;

namespace BO.DTO.Vendor
{
    public class VendorResponseDto
    {
        public int VendorId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public string VendorOwnerName { get; set; }
        public List<BranchResponseDto> Branches { get; set; }
    }
}
