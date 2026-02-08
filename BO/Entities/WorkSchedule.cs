using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BO.Entities
{
    public class WorkSchedule
    {
        [Key]
        public int WorkScheduleId { get; set; }

        [ForeignKey("Branch")]
        public int BranchId { get; set; }

        // Stores the day (e.g., 0=Sunday, 1=Monday... or 1=Sunday depending on your logic)
        public int Weekday { get; set; }
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }

        public virtual Branch Branch { get; set; }
    }
}
