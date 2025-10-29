using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Proposal;

public class ProposalVoteDTO
{
    [Required(ErrorMessage = "ProposalId is required")]
    public int ProposalId { get; set; }
    
    [Required(ErrorMessage = "Vote decision is required")]
    public bool IsAgree { get; set; }
}

