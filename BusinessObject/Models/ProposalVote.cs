using System;

namespace BusinessObject.Models
{
    public class ProposalVote
    {
        public int Id { get; set; }

        public int ProposalId { get; set; }
        public Proposal Proposal { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public bool IsAgree { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

