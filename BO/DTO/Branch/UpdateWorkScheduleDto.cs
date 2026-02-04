using System;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Branch
{
    public class UpdateWorkScheduleDto
    {
        /// <summary>
        /// Day of week (0=Sunday, 1=Monday, 2=Tuesday, 3=Wednesday, 4=Thursday, 5=Friday, 6=Saturday)
        /// </summary>
        [Required]
        [Range(0, 6)]
        public int Weekday { get; set; }

        /// <summary>
        /// Opening time (HH:mm format)
        /// </summary>
        [Required]
        public TimeSpan OpenTime { get; set; }

        /// <summary>
        /// Closing time (HH:mm format)
        /// </summary>
        [Required]
        public TimeSpan CloseTime { get; set; }
    }
}
