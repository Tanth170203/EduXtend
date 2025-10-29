namespace BusinessObject.DTOs.Proposal;

public class ProposalDTO
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string ClubName { get; set; } = null!;
    public int CreatedById { get; set; }
    public string CreatedByName { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int AgreeCount { get; set; }
    public int DisagreeCount { get; set; }
    public bool? CurrentUserVote { get; set; } // null if not voted, true if agree, false if disagree
}

