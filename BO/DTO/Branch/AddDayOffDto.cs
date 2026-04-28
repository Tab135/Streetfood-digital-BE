using System;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Branch
{
    public class AddDayOffDto
    {
        /// <summary>
        /// Start date-time of the day off period
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date-time of the day off period
        /// </summary>
        [Required]
        public DateTime EndDate { get; set; }
    }
}
