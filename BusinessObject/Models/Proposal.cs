using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models
{
    public class Proposal
    {
        public int Id { get; set; }

        public int ClubId { get; set; }
        public Club Club { get; set; } = null!;

        public int CreatedById { get; set; }
        public User CreatedBy { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "PendingVote"; // PendingVote, ApprovedByClub, Rejected

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<ProposalVote> Votes { get; set; } = new List<ProposalVote>();
    }
}

