using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Activity
{
    public class UpdateScoreDto
    {
        [Required(ErrorMessage = "ParticipationScore is required")]
        [Range(3, 5, ErrorMessage = "ParticipationScore must be between 3 and 5")]
        public int ParticipationScore { get; set; }
    }
}
