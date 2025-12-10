using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Activity
{
    public class CreateActivityScheduleDto
    {
        [Required]
        public string StartTime { get; set; } = null!; // HH:mm format
        
        [Required]
        public string EndTime { get; set; } = null!; // HH:mm format
        
        [Required, MaxLength(500)]
        public string Title { get; set; } = null!;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        public List<CreateActivityScheduleAssignmentDto> Assignments { get; set; } = new();
    }
}
