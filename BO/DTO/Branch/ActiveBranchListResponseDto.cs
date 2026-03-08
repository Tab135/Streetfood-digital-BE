using System.Collections.Generic;

namespace BO.DTO.Branch
{
    /// <summary>
    /// Response DTO for active branch filtering.
    /// Returns all branches matching the filter criteria.
    /// </summary>
    public class ActiveBranchListResponseDto
    {
        /// <summary>List of all branches matching the filter.</summary>
        public List<ActiveBranchResponseDto> Items { get; set; } = new();

        /// <summary>Total number of branches returned.</summary>
        public int TotalCount { get; set; }
    }
}
