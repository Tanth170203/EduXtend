using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class EvaluationAuditLog
    {
        public int Id { get; set; }
        public int EvaluationId { get; set; }
        public TrainingEvaluation Evaluation { get; set; } = default!;
        public int ChangedById { get; set; }
        public User ChangedBy { get; set; } = default!;
        public DateTime ChangedAt { get; set; } = DateTime.Now;
        public string OldValue { get; set; } = null!;
        public string NewValue { get; set; } = null!;
        public string Action { get; set; } = null!;
    }
}
