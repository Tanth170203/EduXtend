using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Interview
{
    public class UpdateEvaluationDto
    {
        [Required]
        [MaxLength(2000)]
        public string Evaluation { get; set; } = string.Empty;
    }
}

