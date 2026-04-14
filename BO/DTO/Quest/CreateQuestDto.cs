using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Quest
{
    public class CreateQuestDto
    {
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public bool IsStandalone { get; set; } = true;

        public int? CampaignId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one task is required")]
        public List<CreateQuestTaskDto> Tasks { get; set; } = new();
    }
}
