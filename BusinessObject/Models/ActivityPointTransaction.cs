using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class ActivityPointTransaction
    {
        public int Id { get; set; }
        public int EvaluationId { get; set; }
        public TrainingEvaluation Evaluation { get; set; } = default!;
        public int StudentId { get; set; }
        public Student Student { get; set; } = default!;
        public int ActivityId { get; set; }
        public Activity Activity { get; set; } = default!;
        public int CriterionId { get; set; }
        public TrainingCriterion Criterion { get; set; } = default!;
        public int Points { get; set; }
        [MaxLength(200)] public string Source { get; set; } = "Attendance";
        [MaxLength(500)] public string? Reason { get; set; }
        public int? ApprovedByStaffId { get; set; }
        public Staff? ApprovedByStaff { get; set; }
    }
}
