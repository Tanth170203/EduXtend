using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Activity
{
    public class CreateActivityScheduleAssignmentDto
    {
        public int? UserId { get; set; }
        
        [MaxLength(200)]
        public string? ResponsibleName { get; set; }
        
        [MaxLength(100)]
        public string? Role { get; set; }
    }
}
