using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class TrainingEvaluationPeriod
    {
        public int Id { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = null!;
        public int SemesterId { get; set; }
        public Semester Semester { get; set; } = default!;

        public DateTime SelfEvalStart { get; set; }
        public DateTime SelfEvalEnd { get; set; }
        public DateTime ClassReviewStart { get; set; }
        public DateTime ClassReviewEnd { get; set; }
        public DateTime AdvisorReviewStart { get; set; }
        public DateTime AdvisorReviewEnd { get; set; }
        public DateTime FinalizationDate { get; set; }

        public int? MaxActivityPointsPerStudent { get; set; }
        public ICollection<TrainingEvaluation> Evaluations { get; set; } = new List<TrainingEvaluation>();
    }
}
