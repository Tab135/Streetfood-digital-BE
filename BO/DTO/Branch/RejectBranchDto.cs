using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Branch
{
    public class RejectBranchDto
    {
        [Required(ErrorMessage = "Rejection reason is required")]
        public string Reason { get; set; }
    }
}
