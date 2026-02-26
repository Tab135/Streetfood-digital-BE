using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Branch
{
    public class AddWorkScheduleDto
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one weekday must be selected")]
        public List<int> Weekdays { get; set; } = new();

        [Required]
        public TimeSpan OpenTime { get; set; }

        [Required]
        public TimeSpan CloseTime { get; set; }
    }
}
