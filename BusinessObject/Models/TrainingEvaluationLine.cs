using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class TrainingEvaluationLine 
    {
        public int Id { get; set; }
        public int EvaluationId { get; set; }
        public TrainingEvaluation Evaluation { get; set; } = default!;
        public int CriterionId { get; set; }
        public TrainingCriterion Criterion { get; set; } = default!;

        public int? SelfScore { get; set; }
        public int? ClassScore { get; set; }
        public int? AdvisorScore { get; set; }
        public int? FinalScore { get; set; }
        [MaxLength(1000)] public string? Note { get; set; }

        public ICollection<EvidenceDocument> EvidenceDocuments { get; set; } = new List<EvidenceDocument>();
    }
}
