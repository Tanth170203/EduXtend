using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.JoinRequest
{
    public class ProcessJoinRequestDto
    {
        [Required]
        public string Action { get; set; } = string.Empty; // "Approve" or "Reject"
        
        [MaxLength(500)]
        public string? Note { get; set; }
    }
}

