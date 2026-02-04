using System;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Branch
{
    public class AddDayOffDto
    {
        /// <summary>
        /// Start date of the day off period
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date of the day off period
        /// </summary>
        [Required]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Optional start time for partial day off
        /// </summary>
        public TimeSpan? StartTime { get; set; }

        /// <summary>
        /// Optional end time for partial day off
        /// </summary>
        public TimeSpan? EndTime { get; set; }
    }
}
