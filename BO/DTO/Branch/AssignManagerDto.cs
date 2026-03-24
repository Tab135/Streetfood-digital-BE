using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Branch
{
    public class AssignManagerDto
    {
        [Required]
        public int ManagerId { get; set; }
    }
}
