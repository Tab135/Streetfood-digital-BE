using System.ComponentModel.DataAnnotations;

namespace BO.DTO.GhostPin
{
    public class CreateGhostPinRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string AddressDetail { get; set; }

        public string? Ward { get; set; }

        [Required]
        public string City { get; set; }

        public double Lat { get; set; }

        public double Long { get; set; }
    }
}
