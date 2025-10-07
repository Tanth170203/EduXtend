using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models
{
    public class JoinRequest
    {
        public int Id { get; set; }

        public int ClubId { get; set; }
        public Club Club { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [MaxLength(500)]
        public string? Motivation { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

