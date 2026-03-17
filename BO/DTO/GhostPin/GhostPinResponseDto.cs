using System;

namespace BO.DTO.GhostPin
{
    public class GhostPinResponseDto
    {
        public int GhostPinId { get; set; }
        public int CreatorId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public double Lat { get; set; }
        public double Long { get; set; }
        public string Status { get; set; }
        public string? RejectReason { get; set; }
        public int? LinkedBranchId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
