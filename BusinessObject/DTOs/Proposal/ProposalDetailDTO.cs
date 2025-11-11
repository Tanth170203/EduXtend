namespace BusinessObject.DTOs.Proposal;

public class ProposalDetailDTO
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
    public bool? CurrentUserVote { get; set; }
    public List<VoteInfoDTO> Votes { get; set; } = new();
}

public class VoteInfoDTO
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = null!;
    public bool IsAgree { get; set; }
    public DateTime CreatedAt { get; set; }
}

