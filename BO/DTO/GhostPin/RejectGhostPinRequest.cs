using System.ComponentModel.DataAnnotations;

namespace BO.DTO.GhostPin
{
    public class RejectGhostPinRequest
    {
        [Required]
        public string Reason { get; set; }
    }
}
