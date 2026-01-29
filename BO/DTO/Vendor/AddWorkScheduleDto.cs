using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Vendor
{
    public class AddWorkScheduleDto
    {
        [Required(ErrorMessage = "Weekday is required (0=Sunday, 1=Monday, etc.)")]
        [Range(0, 6, ErrorMessage = "Weekday must be between 0 and 6")]
        public int Weekday { get; set; }

        [Required(ErrorMessage = "Open time is required")]
        public System.TimeSpan OpenTime { get; set; }

        [Required(ErrorMessage = "Close time is required")]
        public System.TimeSpan CloseTime { get; set; }
    }
}
