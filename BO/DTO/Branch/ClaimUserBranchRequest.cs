using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Branch
{
    public class ClaimUserBranchRequest
    {
        [Required]
        public List<string> LicenseUrls { get; set; } = new List<string>();
    }
}