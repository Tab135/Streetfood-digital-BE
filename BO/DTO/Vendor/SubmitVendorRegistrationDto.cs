using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Vendor
{
    public class SubmitVendorRegistrationDto
    {
        [Required(ErrorMessage = "License image is required")]
        public IFormFile LicenseImage { get; set; }
    }
}
