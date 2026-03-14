using System;
using System.Collections.Generic;

namespace BO.DTO.Branch
{
    /// <summary>
    /// Response DTO for an active branch with distance from user and matching dishes.
    /// </summary>
    public class ActiveBranchResponseDto
    {
        public int BranchId { get; set; }
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AddressDetail { get; set; } = string.Empty;
        public string? Ward { get; set; }
        public string City { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Long { get; set; }
        public double AvgRating { get; set; }
        public bool IsVerified { get; set; }

        /// <summary>Distance in kilometers from user's location (null if no location provided)</summary>
        public double? DistanceKm { get; set; }

        /// <summary>Active dishes that match the filter criteria</summary>
        public List<ActiveDishResponseDto> Dishes { get; set; } = new List<ActiveDishResponseDto>();
    }

    /// <summary>
    /// Dish information returned in the active branch response.
    /// </summary>
    public class ActiveDishResponseDto
    {
        public int DishId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsSoldOut { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<string> TasteNames { get; set; } = new List<string>();
        public List<string> DietaryPreferenceNames { get; set; } = new List<string>();
    }
}
