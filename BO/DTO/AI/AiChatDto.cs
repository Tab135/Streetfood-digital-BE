using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.AI
{
    public class AiChatRequestDto
    {
        [Required(ErrorMessage = "Message is required")]
        [MinLength(1, ErrorMessage = "Message cannot be empty")]
        public string Message { get; set; } = string.Empty;

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double? Lat { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double? Long { get; set; }

        [Range(0.1, 500, ErrorMessage = "Distance must be between 0.1 and 500 km")]
        public double? DistanceKm { get; set; }
    }

    public class AiChatHistoryMessageDto
    {
        public string Role { get; set; } = "user";

        public string Content { get; set; } = string.Empty;
    }

    public class AiChatResponseDto
    {
        public string Intent { get; set; } = "chat";
        public string Reply { get; set; } = string.Empty;
        public AiRecommendationQueryDto Query { get; set; } = new();
        public int MatchedBranchCount { get; set; }
        public List<AiRecommendedBranchDto> RecommendedBranches { get; set; } = new();
    }

    public class AiRecommendationQueryDto
    {
        public string? Keyword { get; set; }
        public List<string> SearchTerms { get; set; } = new();
        public double? Lat { get; set; }
        public double? Long { get; set; }
        public double? DistanceKm { get; set; }
        public List<int> DietaryIds { get; set; } = new();
        public List<int> TasteIds { get; set; } = new();
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public List<int> CategoryIds { get; set; } = new();
    }

    public class AiRecommendedBranchDto
    {
        public int BranchId { get; set; }
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AddressDetail { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? Ward { get; set; }
        public double AvgRating { get; set; }
        public double? DistanceKm { get; set; }
        public double FinalScore { get; set; }
        public List<string> DietaryPreferenceNames { get; set; } = new();
        public List<AiRecommendedDishDto> RecommendedDishes { get; set; } = new();
    }

    public class AiRecommendedDishDto
    {
        public int DishId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
