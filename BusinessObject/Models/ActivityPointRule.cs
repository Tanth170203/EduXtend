using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ActivityPointRule
    {
        public int Id { get; set; }

        // Nếu rule gắn trực tiếp với 1 activity
        public int? ActivityId { get; set; }
        public Activity? Activity { get; set; }

        // Nếu rule áp dụng theo loại hoạt động (lookup)
        [MaxLength(100)] public string? ActivityTypeKey { get; set; }

        public int CriterionId { get; set; }
        public TrainingCriterion Criterion { get; set; } = default!;

        public int Points { get; set; }
        public bool RequiresAttendance { get; set; } = true;
        public bool RequiresEvidenceApproval { get; set; } = false;

    }
}
