using BusinessObject.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class Appeal
    {
        public int Id { get; set; }
        public int EvaluationId { get; set; }
        public TrainingEvaluation Evaluation { get; set; } = default!;
        public int StudentId { get; set; }
        public Student Student { get; set; } = default!;

        [Required, MaxLength(1000)] public string Reason { get; set; } = default!;
        public AppealStatus Status { get; set; } = AppealStatus.Pending;
        public DateTime SubmittedAt { get; set; } = DateTime.Now;
        public DateTime? ResolvedAt { get; set; }
        [MaxLength(1000)] public string? ResolutionNote { get; set; }
    }
}
