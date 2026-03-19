using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Branch
{
    public class RejectUserBranchRequest
    {
        [Required]
        public string Reason { get; set; }
    }
}