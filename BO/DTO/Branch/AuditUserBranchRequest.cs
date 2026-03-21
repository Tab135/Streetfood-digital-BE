using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Branch
{
    public class AuditUserBranchRequest
    {
        public double ModLat { get; set; }
        public double ModLong { get; set; }
    }
}