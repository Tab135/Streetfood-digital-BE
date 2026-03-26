using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Quest
{
    public class UpdateQuestDto
    {
        [StringLength(255)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsActive { get; set; }
        public int? CampaignId { get; set; }

        public List<CreateQuestTaskDto>? Tasks { get; set; }
    }
}
