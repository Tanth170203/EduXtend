using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Models
{
    public class EvidenceDocument
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; } = default!;
        [Required, MaxLength(260)] public string FileName { get; set; } = default!;
        [Required, MaxLength(1000)] public string Url { get; set; } = default!;
        [MaxLength(200)] public string? ContentType { get; set; }
        public long SizeBytes { get; set; }

        public int? EvaluationLineId { get; set; }
        public TrainingEvaluationLine? EvaluationLine { get; set; }
        public int? ActivityRegistrationId { get; set; }
        public ActivityRegistration? ActivityRegistration { get; set; }
    }
}
