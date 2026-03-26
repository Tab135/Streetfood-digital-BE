namespace BO.DTO.Quest
{
    public class QuestTaskResponseDto
    {
        public int QuestTaskId { get; set; }
        public string Type { get; set; } = string.Empty;
        public int TargetValue { get; set; }
        public string? Description { get; set; }
        public string RewardType { get; set; } = string.Empty;
        public int RewardValue { get; set; }
    }
}
