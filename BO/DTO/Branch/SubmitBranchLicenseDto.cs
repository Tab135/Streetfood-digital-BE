using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Branch
{
    /// <summary>
    /// DTO for submitting a license image for branch verification
    /// </summary>
    public class SubmitBranchLicenseDto
    {
        [Required(ErrorMessage = "License image is required")]
        public IFormFile LicenseImage { get; set; }
    }
}
