using BO.DTO.Users;
using System.Collections.Generic;

namespace BO.DTO.Quest
{
    public class UserQuestTaskGroupedDto
    {
        public int UserQuestId { get; set; }
        public int UserId { get; set; }
        public UserProfileDto User { get; set; } = new();
        public int QuestId { get; set; }
        public string QuestTitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<UserQuestTaskProgressDto> Tasks { get; set; } = new();
    }
}
