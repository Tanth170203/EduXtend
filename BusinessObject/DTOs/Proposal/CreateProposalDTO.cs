using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Proposal;

public class CreateProposalDTO
{
    [Required(ErrorMessage = "Club ID is required")]
    public int ClubId { get; set; }
    
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = null!;
    
    [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }
}

