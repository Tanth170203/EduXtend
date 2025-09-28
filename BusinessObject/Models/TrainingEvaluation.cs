using BusinessObject.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class TrainingEvaluation
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; } = default!;
        public int PeriodId { get; set; }
        public TrainingEvaluationPeriod Period { get; set; } = default!;

        public EvaluationStatus Status { get; set; } = EvaluationStatus.Draft;
        public int? TotalSelfScore { get; set; }
        public int? TotalClassScore { get; set; }
        public int? TotalAdvisorScore { get; set; }
        public int? TotalFinalScore { get; set; }

        public ICollection<TrainingEvaluationLine> Lines { get; set; } = new List<TrainingEvaluationLine>();
        public ICollection<Appeal> Appeals { get; set; } = new List<Appeal>();
        public ICollection<ActivityPointTransaction> ActivityPointTransactions { get; set; } = new List<ActivityPointTransaction>();
        public ICollection<EvaluationAuditLog> AuditLogs { get; set; } = new List<EvaluationAuditLog>();

    }
}
