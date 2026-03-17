using System.ComponentModel.DataAnnotations;

namespace BO.DTO.GhostPin
{
    public class AuditGhostPinRequest
    {
        public double ModLat { get; set; }
        public double ModLong { get; set; }
    }
}
