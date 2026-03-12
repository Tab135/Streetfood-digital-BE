using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Branch
{
    /// <summary>
    /// Request DTO for filtering active branches.
    /// Returns all branches matching the criteria.
    /// </summary>
    public class ActiveBranchFilterDto
    {
        /// <summary>User's current latitude (optional, defaults to HCM center: 10.8231)</summary>
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double? Lat { get; set; }

        /// <summary>User's current longitude (optional, defaults to HCM center: 106.6297)</summary>
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double? Long { get; set; }

        /// <summary>Maximum search radius in kilometers. Default: 10km</summary>
        [Range(0.1, 500, ErrorMessage = "Distance must be between 0.1 and 500 km")]
        public double? Distance { get; set; }

        /// <summary>Array of dietary preference IDs to filter dishes. Ignored if null or empty.</summary>
        public List<int>? DietaryIds { get; set; }

        /// <summary>Array of taste IDs to filter dishes. Ignored if null or empty.</summary>
        public List<int>? TasteIds { get; set; }

        /// <summary>Minimum dish price filter. Ignored if null.</summary>
        [Range(0, double.MaxValue, ErrorMessage = "MinPrice must be a positive value")]
        public decimal? MinPrice { get; set; }

        /// <summary>Maximum dish price filter. Ignored if null.</summary>
        [Range(0, double.MaxValue, ErrorMessage = "MaxPrice must be a positive value")]
        public decimal? MaxPrice { get; set; }

        /// <summary>Array of category IDs to filter dishes. Ignored if null or empty.</summary>
        public List<int>? CategoryIds { get; set; }
    }
}
